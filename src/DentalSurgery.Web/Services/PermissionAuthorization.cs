using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DentalSurgery.Web.Services;

/// <summary>An authorisation requirement for one named permission.</summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>
/// Builds a policy on demand for any permission name.
/// <para>
/// Without this every one of the permissions in <see cref="Permissions"/> would
/// have to be registered by hand at startup, and adding a permission would mean
/// remembering to register it — a failure that shows up as a page nobody can
/// open, or worse, as <c>[Authorize(Policy = "…")]</c> naming a policy that does
/// not exist. ASP.NET treats an unknown policy as a configuration error, so the
/// provider below turns any string the application recognises as a permission
/// into a real requirement, and refuses anything it does not recognise.
/// </para>
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // An explicitly registered policy always wins.
        var configured = await base.GetPolicyAsync(policyName);
        if (configured is not null) return configured;

        if (!Permissions.IsDefined(policyName)) return null;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}

/// <summary>
/// Decides a <see cref="PermissionRequirement"/> by resolving the principal's
/// roles through the role-to-permission map.
/// </summary>
public class PermissionAuthorizationHandler(PermissionCatalogue catalogue)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roles.Length == 0) return;

        // Roles are per-tenant, so a role name alone does not identify a grant.
        // The tenant comes from the principal's own claim: the same signed value
        // the request's scope is bound to.
        if (!Guid.TryParse(context.User.FindFirstValue(DentalClaimTypes.TenantId), out var tenantId)
            || tenantId == Guid.Empty)
        {
            return;
        }

        var held = await catalogue.ForRolesAsync(tenantId, roles);

        if (held.Contains(requirement.Permission)) context.Succeed(requirement);
    }
}

/// <summary>
/// Requires that the account is a member of staff: signed in, and holding at
/// least one permission through some role.
/// <para>
/// This is what a bare <c>[Authorize]</c> should have meant. A signed-in account
/// with no roles is not a member of staff — it is either a provisioning
/// mistake or someone who obtained an account they should not have — and it
/// gets no further than the access-denied screen.
/// </para>
/// </summary>
public class StaffMemberRequirement : IAuthorizationRequirement;

public class StaffMemberHandler(PermissionCatalogue catalogue)
    : AuthorizationHandler<StaffMemberRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, StaffMemberRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roles.Length == 0) return;

        if (!Guid.TryParse(context.User.FindFirstValue(DentalClaimTypes.TenantId), out var tenantId)
            || tenantId == Guid.Empty)
        {
            return;
        }

        var held = await catalogue.ForRolesAsync(tenantId, roles);

        if (held.Count > 0) context.Succeed(requirement);
    }
}
