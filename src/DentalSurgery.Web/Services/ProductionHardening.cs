using DentalSurgery.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

namespace DentalSurgery.Web.Services;

/// <summary>
/// The cross-cutting pieces a deployment needs and a development run does not:
/// a shared key ring, proxy awareness, request throttling, response headers and
/// health probes.
/// </summary>
public static class ProductionHardening
{
    public const string SignInPolicy = "sign-in";
    public const string ApiPolicy = "api";

    // ---------------------------------------------------------------- keys

    /// <summary>
    /// Puts the data protection key ring in the database and, where a
    /// certificate is configured, encrypts it at rest.
    /// <para>
    /// By default ASP.NET Core writes these keys to the local user profile.
    /// Every host then has its own set, so an authentication cookie or antiforgery
    /// token issued by one instance is unreadable by the next: users are signed
    /// out apparently at random as requests move between hosts, and every
    /// restart invalidates every session. Sharing the ring through the database
    /// the application already owns removes both problems without adding an
    /// external dependency.
    /// </para>
    /// </summary>
    public static IServiceCollection AddSharedDataProtection(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var applicationName = configuration["DataProtection:ApplicationName"] ?? "DentalSurgery";

        var builder = services.AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToDbContext<DentalDbContext>();

        var thumbprint = configuration["DataProtection:CertificateThumbprint"];

        if (!string.IsNullOrWhiteSpace(thumbprint))
        {
            var certificate = FindCertificate(thumbprint);

            if (certificate is null)
            {
                throw new InvalidOperationException(
                    $"DataProtection:CertificateThumbprint '{thumbprint}' was not found in the " +
                    "LocalMachine or CurrentUser store. The key ring cannot be encrypted at rest.");
            }

            builder.ProtectKeysWithCertificate(certificate);
        }
        else if (environment.IsProduction())
        {
            // Not fatal: the rows are inside the practice database, which is
            // itself protected. It is still worth stating plainly, because an
            // unencrypted key ring means a database backup carries the material
            // needed to forge a session cookie.
            var logger = LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger(nameof(ProductionHardening));

            logger.LogWarning(
                "The data protection key ring is stored unencrypted. Anyone who can read a database " +
                "backup can mint a valid session cookie. Set DataProtection:CertificateThumbprint.");
        }

        return services;
    }

    private static X509Certificate2? FindCertificate(string thumbprint)
    {
        foreach (var location in new[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser })
        {
            using var store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);

            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint.Replace(" ", string.Empty), validOnly: false);

            if (found.Count > 0) return found[0];
        }

        return null;
    }

    // ---------------------------------------------------------------- proxying

    /// <summary>
    /// Trusts the forwarding headers of the load balancer in front of the app.
    /// <para>
    /// Without this every request appears to arrive from the proxy over plain
    /// HTTP. That breaks the HTTPS redirect, makes the secure-cookie policy
    /// refuse to issue a session, and records the proxy's address against every
    /// audit row and failed sign-in — which is precisely the field an
    /// investigation needs.
    /// </para>
    /// </summary>
    public static IServiceCollection AddProxyAwareness(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // The proxy is inside the deployment's own network and its address
            // is not known ahead of time. Clearing these accepts the header from
            // the immediate peer, which is correct when nothing but the load
            // balancer can reach the container.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    // ---------------------------------------------------------------- throttling

    /// <summary>
    /// Rate limits sign-in and the API.
    /// <para>
    /// Account lockout stops five failures against <em>one</em> account. It does
    /// nothing about one address trying one password against every account it
    /// can name, which is how credential stuffing actually works. This limits by
    /// address instead, so the attack is bounded regardless of how many accounts
    /// it spreads across.
    /// </para>
    /// </summary>
    public static IServiceCollection AddRequestThrottling(
        this IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue("RateLimiting:Enabled", true)) return services;

        var signInPerMinute = configuration.GetValue("RateLimiting:SignInPermitPerMinute", 10);
        var apiPerMinute = configuration.GetValue("RateLimiting:ApiPermitPerMinute", 300);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(SignInPolicy, context =>
            {
                // Only the submission is throttled. Rendering the form is a GET,
                // and counting those would lock a user out of the page itself
                // after a few reloads or a Blazor circuit reconnect — denying
                // service to the honest user while barely inconveniencing an
                // attacker, who only ever needs to POST.
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    return RateLimitPartition.GetNoLimiter("sign-in:read");
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"sign-in:{ClientKey(context)}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = signInPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(ApiPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ClientKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = apiPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("DentalSurgery.RateLimiting");

                logger.LogWarning(
                    "Rate limit reached for {Client} on {Path}.",
                    ClientKey(context.HttpContext), context.HttpContext.Request.Path);

                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Try again in a minute.", token);
            };
        });

        return services;
    }

    /// <summary>
    /// Partitions by authenticated user where there is one, and by address
    /// otherwise, so that a busy shared practice connection cannot exhaust the
    /// allowance of everyone behind it once they have signed in.
    /// </summary>
    private static string ClientKey(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.Identity.Name}"
            : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    /// <summary>
    /// Credential-guessing endpoints, which need the strict limit. These are
    /// Razor Component pages rather than controller actions, so the policy is
    /// attached by convention to the matching routes instead of by attribute.
    /// </summary>
    private static readonly string[] CredentialRoutes =
    [
        "/Account/Login",
        "/Account/ForgotPassword",
        "/Account/ResetPassword",
        "/Account/LoginWith2fa",
        "/Account/LoginWithRecoveryCode",
        "/Account/Register",
        "/Account/ResendEmailConfirmation"
    ];

    /// <summary>
    /// Applies <see cref="SignInPolicy"/> to the credential pages only. Applying
    /// it to every component route would throttle ordinary navigation, and
    /// leaving it off entirely would leave sign-in limited by account lockout
    /// alone.
    /// </summary>
    public static RazorComponentsEndpointConventionBuilder ApplySignInRateLimit(
        this RazorComponentsEndpointConventionBuilder builder)
    {
        builder.Add(endpoint =>
        {
            if (endpoint is not RouteEndpointBuilder route) return;

            var pattern = route.RoutePattern.RawText;
            if (string.IsNullOrEmpty(pattern)) return;

            if (CredentialRoutes.Any(r => pattern.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
            {
                endpoint.Metadata.Add(new EnableRateLimitingAttribute(SignInPolicy));
            }
        });

        return builder;
    }

    // ---------------------------------------------------------------- headers

    /// <summary>
    /// Adds the response headers that constrain what a browser will do with the
    /// page. These matter more than usual here: a clinical record on screen is
    /// exactly what a clickjacking or injected-script attack would target.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

            // A patient record must never be held in a shared or browser cache.
            if (!context.Request.Path.StartsWithSegments("/_framework")
                && !context.Request.Path.StartsWithSegments("/_content")
                && context.Request.Path.Value?.Contains('.') != true)
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }

            // Blazor Server needs its own script, a WebSocket back to the origin
            // and inline styles for scoped CSS and the style attributes the
            // components use. Everything else is denied, including framing and
            // any form posting anywhere but back here.
            headers["Content-Security-Policy"] = string.Join("; ",
                "default-src 'self'",
                "script-src 'self' 'wasm-unsafe-eval'",
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: blob:",
                "font-src 'self' data:",
                "connect-src 'self' ws: wss:",
                "object-src 'none'",
                "frame-ancestors 'none'",
                "base-uri 'self'",
                "form-action 'self'");

            await next();
        });

    // ---------------------------------------------------------------- health

    /// <summary>
    /// Liveness and readiness, kept apart on purpose.
    /// <para>
    /// An orchestrator restarts a container that fails liveness and merely stops
    /// routing to one that fails readiness. Reporting a database outage as a
    /// liveness failure would make every instance restart in a loop while the
    /// database recovers, turning a brief outage into a longer one.
    /// </para>
    /// </summary>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<DentalDbContext>(
                name: "database",
                tags: ["ready"]);

        return services;
    }

    public static void MapApplicationHealthChecks(this WebApplication app)
    {
        // Liveness: the process is up and the pipeline responds. No dependencies.
        app.MapHealthChecks("/health/live", new()
        {
            Predicate = _ => false
        }).AllowAnonymous();

        // Readiness: it can actually serve, database included.
        app.MapHealthChecks("/health/ready", new()
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();
    }
}
