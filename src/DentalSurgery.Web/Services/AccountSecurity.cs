using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DentalSurgery.Web.Services;

/// <summary>
/// Refuses a sign-in for a deactivated account.
/// <para>
/// Deactivating a login is how a practice offboards someone who has left, so it
/// has to be the control it appears to be. <c>IsActive</c> was previously
/// recorded and shown in the staff directory but never consulted, which meant a
/// departed employee kept working access until their password was changed.
/// </para>
/// </summary>
public class DentalSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(
        userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    /// <summary>
    /// Resolves the account before a tenant is known, then signs in inside it.
    /// <para>
    /// The <c>Users</c> set is tenant-filtered like everything else, so on an
    /// anonymous request — which every sign-in is — a lookup by name matches
    /// nothing and the password is never even checked. The account has to be
    /// found first, and the tenant learned from it.
    /// </para>
    /// <para>
    /// Where the host already identified a tenant (a practice's own subdomain)
    /// the lookup stays inside it, which is what allows the same person to hold
    /// an account at two practices under one email address. Without that hint
    /// the search covers every tenant, and an address held by more than one is
    /// refused rather than guessed: signing the wrong person in would be far
    /// worse than asking them to use their practice's address.
    /// </para>
    /// </summary>
    public override async Task<SignInResult> PasswordSignInAsync(
        string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var tenantContext = Context.RequestServices.GetRequiredService<TenantContext>();

        ApplicationUser? user;

        if (tenantContext.TenantId is not null)
        {
            user = await UserManager.FindByNameAsync(userName);
        }
        else
        {
            var normalised = UserManager.NormalizeName(userName);

            using var _ = tenantContext.EnterPlatformScope("resolving an account for sign-in");

            var matches = await UserManager.Users
                .Where(u => u.NormalizedUserName == normalised)
                .Take(2)
                .ToListAsync();

            if (matches.Count > 1)
            {
                Logger.LogWarning(
                    "Sign-in refused for {Email}: the address exists at more than one practice and the " +
                    "host did not identify which.", userName);
                return SignInResult.Failed;
            }

            user = matches.SingleOrDefault();
        }

        // Same shape as the base implementation: an unknown account is a plain
        // failure, indistinguishable from a wrong password.
        if (user is null) return SignInResult.Failed;

        return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }

    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            Logger.LogWarning("Sign-in refused for {Email}: the account is deactivated.", user.Email);
            return false;
        }

        // A suspended tenant is refused at the door rather than having its data
        // hidden. Hiding it would present to the practice as their records
        // having disappeared, which is both alarming and untrue.
        var tenants = Context.RequestServices.GetRequiredService<TenantProvisioningService>();
        var tenant = await tenants.FindByIdAsync(user.TenantId);

        if (tenant is null)
        {
            Logger.LogError(
                "Sign-in refused for {Email}: the account references tenant {Tenant}, which does not exist.",
                user.Email, user.TenantId);
            return false;
        }

        if (!tenant.IsActive)
        {
            Logger.LogWarning(
                "Sign-in refused for {Email}: tenant {Tenant} is suspended.", user.Email, tenant.Name);
            return false;
        }

        // Bind the scope now, before the password is checked.
        //
        // Identity writes to the user row during sign-in - the access-failed
        // count on a bad password, the stamp refresh on a good one - and the
        // tenant guard refuses a write it cannot attribute to a tenant. Until
        // this point the request is unauthenticated and carries no tenant claim,
        // so those writes would be rejected and every sign-in would fail. The
        // identity of the account is known here, which is exactly when its
        // tenant becomes knowable.
        Context.RequestServices.GetRequiredService<TenantContext>().Bind(user.TenantId);

        return await base.CanSignInAsync(user);
    }
}

/// <summary>
/// Rejects the sign-in cookie of an account deactivated since it was issued.
/// <para>
/// Identity's security-stamp validation only catches this if whoever
/// deactivated the account also reset the stamp. This closes the gap directly,
/// so an account switched off in the staff directory loses access on its next
/// request rather than at the end of its cookie lifetime.
/// </para>
/// </summary>
public static class AccountSecurityCookieEvents
{
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true) return;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return;

        // Bind the tenant here, at the earliest point it is knowable.
        //
        // Cookie validation runs inside the authentication middleware, before
        // anything else in the pipeline, and the Users set is tenant-filtered
        // like every other table. Looking the account up before binding finds
        // nothing and rejects a perfectly good session on every request. The
        // signed cookie already names the tenant, so bind from it and let the
        // lookup happen inside that tenant: an account whose row lives in a
        // different tenant than its claim then genuinely is not found, which is
        // the correct answer.
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<TenantContext>();

        if (Guid.TryParse(principal.FindFirstValue(DentalClaimTypes.TenantId), out var tenantId)
            && tenantId != Guid.Empty)
        {
            tenantContext.Bind(tenantId);
        }

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DentalSurgery.AccountSecurity");

            logger.LogWarning(
                "Rejecting the session for {UserId}: the account is {State}.",
                userId, user is null ? "missing" : "deactivated");

            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    }
}

/// <summary>
/// Holds a user on the change-password screen until they have set their own
/// password.
/// <para>
/// <c>MustChangePassword</c> was stored on the account and written to the
/// schema but never consulted, so the flag did nothing: the bootstrap
/// administrator's password stayed valid indefinitely. This makes the flag
/// mean what it says by short-circuiting every other request while it is set.
/// </para>
/// </summary>
public class MustChangePasswordMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Paths that must stay reachable, or the user could neither change the
    /// password nor sign out, and the browser could not load the page's assets.
    /// </summary>
    private static readonly string[] Permitted =
    [
        "/Account/Manage/ChangePassword",
        "/Account/Logout",
        "/Account/Login",
        "/Account/AccessDenied",
        "/_blazor",
        "/_framework",
        "/_content",
        "/css",
        "/js",
        "/bootstrap",
        "/lib",
        "/health"
    ];

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path;

        if (Permitted.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
            || path.Value?.Contains('.') == true)
        {
            await next(context);
            return;
        }

        var user = await userManager.GetUserAsync(context.User);

        if (user?.MustChangePassword == true)
        {
            // An API caller gets a status it can act on; a browser gets the page.
            if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "A password change is required.",
                    detail = "This account must set a new password before it can be used.",
                    status = 403
                });
                return;
            }

            context.Response.Redirect("/Account/Manage/ChangePassword?forced=1");
            return;
        }

        await next(context);
    }
}

public static class AccountSecurityExtensions
{
    public static IApplicationBuilder UseMustChangePassword(this IApplicationBuilder app) =>
        app.UseMiddleware<MustChangePasswordMiddleware>();
}
