using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DentalSurgery.Web.Services;

/// <summary>Extra claims stamped onto the sign-in cookie.</summary>
public static class DentalClaimTypes
{
    /// <summary>
    /// The tenant this session is confined to. Stamped at sign-in and read on
    /// every request; it is the single value the whole isolation model turns on.
    /// </summary>
    public const string TenantId = "dental:tenant_id";

    public const string StaffId = "dental:staff_id";
    public const string DisplayName = "dental:display_name";
    public const string LocationId = "dental:location_id";
    public const string JobTitle = "dental:job_title";

    /// <summary>
    /// Present while the account still owes a password change. Carried on the
    /// cookie so the shell can strip itself back to the one thing the user is
    /// allowed to do, without a database round trip on every render.
    /// </summary>
    public const string MustChangePassword = "dental:must_change_password";
}

/// <summary>
/// Adds the staff link and display name to the principal at sign-in, so the
/// rest of the application can attribute clinical records without a database
/// round trip on every request.
/// </summary>
public class DentalClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(DentalClaimTypes.DisplayName, user.DisplayName));

        // Written even when empty, so a malformed account produces a claim that
        // resolves to no tenant - and therefore sees nothing - rather than no
        // claim at all, which is harder to reason about downstream.
        identity.AddClaim(new Claim(DentalClaimTypes.TenantId, user.TenantId.ToString()));

        if (user.StaffId is { } staffId)
            identity.AddClaim(new Claim(DentalClaimTypes.StaffId, staffId.ToString()));

        if (user.DefaultLocationId is { } locationId)
            identity.AddClaim(new Claim(DentalClaimTypes.LocationId, locationId.ToString()));

        if (!string.IsNullOrWhiteSpace(user.JobTitle))
            identity.AddClaim(new Claim(DentalClaimTypes.JobTitle, user.JobTitle));

        if (user.MustChangePassword)
            identity.AddClaim(new Claim(DentalClaimTypes.MustChangePassword, "true"));

        return identity;
    }
}

/// <summary>
/// Resolves the operator for auditing. Static server rendering reads the
/// HTTP context; an interactive circuit reads the cascading authentication
/// state, which the shell publishes when the circuit starts.
/// </summary>
public class WebCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider) : ICurrentUser
{
    private ClaimsPrincipal? Principal
    {
        get
        {
            var fromHttp = httpContextAccessor.HttpContext?.User;
            if (fromHttp?.Identity?.IsAuthenticated == true) return fromHttp;

            try
            {
                // In an interactive circuit the state is already materialised.
                var task = authenticationStateProvider.GetAuthenticationStateAsync();
                return task.IsCompletedSuccessfully ? task.Result.User : fromHttp;
            }
            catch (InvalidOperationException)
            {
                // Resolved outside a component scope, for example by a startup
                // task or a background job. There is no signed-in operator.
                return fromHttp;
            }
        }
    }

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        Principal?.FindFirstValue(ClaimTypes.Name) ??
        Principal?.FindFirstValue(ClaimTypes.Email);

    public string? DisplayName =>
        Principal?.FindFirstValue(DentalClaimTypes.DisplayName) ?? UserName;

    public Guid? StaffId =>
        Guid.TryParse(Principal?.FindFirstValue(DentalClaimTypes.StaffId), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
