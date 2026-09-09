using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Persistence.Seed;
using DentalSurgery.Web.Components;
using DentalSurgery.Web.Components.Account;
using DentalSurgery.Infrastructure.Services;
using DentalSurgery.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;

// QuestPDF is free under its community licence below the revenue threshold.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- logging
// Structured, so that a production incident can be queried rather than read.
// Console output is JSON in a deployment, where a log shipper consumes it, and
// human-readable text locally.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "DentalSurgery")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning);

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console();
    }
    else
    {
        configuration.WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    }
});

// Uploads are capped in DocumentService; this raises the transport limit to match.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = DocumentService.MaxUploadBytes + (1024 * 1024);
    options.ValueLengthLimit = int.MaxValue;
});

// ---------------------------------------------------------------- presentation
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddControllers(options =>
    {
        // A refusal from the service layer becomes a 403, not a 500.
        options.Filters.Add<ForbiddenExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Enums are written as names, so they must also be read as names.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddScoped<ForbiddenExceptionFilter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dental Surgery API",
        Version = "v1",
        Description = "Read and write access to patients, the appointment book, clinical records and billing."
    });
});

// ---------------------------------------------------------------- identity
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ICurrentUser, WebCurrentUser>();

// A Blazor circuit has its own scope and never re-enters the middleware
// pipeline, so it needs its tenant bound separately from the HTTP request.
builder.Services.AddScoped<CircuitHandler, TenantCircuitHandler>();

// Registered before AddDentalInfrastructure, whose TryAdd fallback would
// otherwise install the permissive system guard used by the seeder.
builder.Services.AddScoped<IPermissionGuard, PermissionGuard>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // The session cookie carries access to patient records. It must never be
    // readable by script, never travel unencrypted, and never ride along on a
    // cross-site request.
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;

    // The __Host- prefix is the strongest binding a cookie can have: the browser
    // enforces Secure, host-only scope and Path=/, so a neighbouring subdomain
    // cannot overwrite the session. It also means the browser rejects the cookie
    // outright over plain HTTP, which is why the development profile — served on
    // http://localhost — uses an unprefixed name instead.
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "DentalSurgery.Session";
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Name = "__Host-DentalSurgery.Session";
    }

    options.Events.OnValidatePrincipal = AccountSecurityCookieEvents.ValidatePrincipalAsync;

    // A browser is redirected to sign in or to the access-denied page; an API
    // caller is given the status code it can act on. Without this an
    // unauthorised /api request answers 200 with a page of HTML, which a client
    // has no way to tell apart from success.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// ---------------------------------------------------------------- infrastructure
builder.Services.AddDentalInfrastructure(builder.Configuration);

// ---------------------------------------------------------------- hardening
builder.Services.AddSharedDataProtection(builder.Configuration, builder.Environment);
builder.Services.AddProxyAwareness();
builder.Services.AddRequestThrottling(builder.Configuration);
builder.Services.AddApplicationHealthChecks();

builder.Services.AddIdentityCore<ApplicationUser>(DependencyInjection.ConfigureIdentityOptions)
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<DentalDbContext>()
    .AddSignInManager<DentalSignInManager>()
    .AddClaimsPrincipalFactory<DentalClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// Scoped, not singleton: it sends through the practice's configured SMTP
// gateway, which is a scoped service.
builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------------------------------------------------------------- authorisation
//
// Policies are permission names, resolved at request time through the
// role-to-permission map rather than by testing role names here. Adding a role,
// or changing what an existing one may do, is a data change; it does not touch
// this file.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, StaffMemberHandler>();

builder.Services.AddAuthorizationBuilder()
    // A bare [Authorize] means "a member of staff", not merely "holds a cookie".
    // An account with no role grants nothing, so a self-provisioned or
    // half-configured login cannot read a patient record by signing in.
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new StaffMemberRequirement())
        .Build())
    .AddPolicy(Policies.StaffMember, p => p
        .RequireAuthenticatedUser()
        .AddRequirements(new StaffMemberRequirement()));

var rateLimitingEnabled = builder.Configuration.RateLimitingEnabled();

var app = builder.Build();

// ---------------------------------------------------------------- database
await using (var scope = app.Services.CreateAsyncScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>();
    await initialiser.InitialiseAsync();
}

// ---------------------------------------------------------------- pipeline

// First in the pipeline: everything downstream needs the real scheme and
// client address rather than the load balancer's.
app.UseForwardedHeaders();

// One structured line per request. Serilog's RequestPath is the path only, so
// the query string never reaches the log — which matters here, because the
// patient search submits names as a query parameter and those must not be
// written to a log shipper.
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, exception) =>
        exception is not null ? LogEventLevel.Error
        : httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
        : httpContext.Response.StatusCode == 429 ? LogEventLevel.Warning
        // Health probes fire constantly and would drown everything else.
        : httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
        : LogEventLevel.Information;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());

        if (httpContext.User.Identity?.IsAuthenticated == true)
            diagnosticContext.Set("User", httpContext.User.Identity.Name);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Dental Surgery API v1");
        o.RoutePrefix = "api-docs";
        o.DocumentTitle = "Dental Surgery API";
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseStaticFiles();

// Only when the limiter was actually registered. UseRateLimiter throws if it
// was not, which turned "RateLimiting:Enabled=false" into a start-up crash.
if (rateLimitingEnabled) app.UseRateLimiter();

app.UseAuthentication();

// Immediately after authentication and before anything that queries: until the
// scope is bound to a tenant the query filters match nothing, so this must run
// ahead of authorisation, the endpoints and the Blazor circuit.
app.UseTenantResolution();

app.UseAuthorization();

// After authentication, and it must stay there.
//
// An antiforgery token is bound to the user it was issued to. Validating it
// before the authentication middleware has resolved HttpContext.User compares
// the token against an anonymous principal, so every form posted by a signed-in
// user is rejected with "the provided antiforgery token was meant for a
// different claims-based user". Anonymous posts such as sign-in still succeed,
// which is what makes the fault look intermittent rather than total.
app.UseAntiforgery();

// After authentication, so it knows who the user is; before the endpoints, so
// an account owing a password change cannot reach any of them.
app.UseMustChangePassword();

app.MapApplicationHealthChecks();

var controllers = app.MapControllers();
if (rateLimitingEnabled) controllers.RequireRateLimiting(ProductionHardening.ApiPolicy);

var components = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (rateLimitingEnabled) components.ApplySignInRateLimit();

app.MapAdditionalIdentityEndpoints();

app.Run();

/// <summary>
/// Top-level statements compile to an internal <c>Program</c>, which
/// <c>WebApplicationFactory&lt;T&gt;</c> cannot reach. Declaring it here makes the
/// real composition root — this file, with its actual middleware order and its
/// actual authorisation wiring — the thing the integration tests boot, rather
/// than a second host assembled to look like it.
/// </summary>
public partial class Program;
