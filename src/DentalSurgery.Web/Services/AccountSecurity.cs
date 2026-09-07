using DentalSurgery.Infrastructure.Identity;
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
    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            Logger.LogWarning("Sign-in refused for {Email}: the account is deactivated.", user.Email);
            return false;
        }

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
