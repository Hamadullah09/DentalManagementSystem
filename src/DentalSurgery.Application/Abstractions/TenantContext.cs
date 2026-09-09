namespace DentalSurgery.Application.Abstractions;

/// <summary>
/// The tenant every query and write in the current scope is confined to.
/// <para>
/// Resolved once per request from the signed-in user and then treated as
/// immutable. Application code never passes a tenant id around and never
/// filters by it: the persistence layer applies it, so a query that forgets
/// about tenancy returns nothing rather than everything.
/// </para>
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant, or null when there is no tenant in scope — an
    /// anonymous request, or a platform operation.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// True when this scope is deliberately operating across tenants: database
    /// seeding, migrations, the reminder worker sweeping every tenant's due
    /// messages, and platform administration.
    /// <para>
    /// This suspends the query filter, so it is the one dangerous flag in the
    /// system. It cannot be set from a request; only server-side code that
    /// calls <see cref="ITenantScopeFactory"/> can enter such a scope.
    /// </para>
    /// </summary>
    bool IsPlatformScope { get; }

    /// <summary>
    /// The tenant to stamp on new rows. Throws when there is none, rather than
    /// writing <c>Guid.Empty</c> and creating a row that belongs to nobody and
    /// is visible to no one.
    /// </summary>
    Guid RequireTenantId();
}

/// <summary>
/// Enters a tenant scope explicitly. Used by code that runs outside a request
/// and therefore has no user to resolve a tenant from.
/// </summary>
public interface ITenantScopeFactory
{
    /// <summary>
    /// Confines the current scope to one tenant. Used by background work that
    /// processes each tenant in turn.
    /// </summary>
    IDisposable EnterTenant(Guid tenantId);

    /// <summary>
    /// Suspends tenant filtering for the current scope.
    /// <para>
    /// Only for genuine platform operations — seeding reference catalogues,
    /// provisioning a tenant, enumerating tenants for a background sweep.
    /// Never in response to a request, and never as a convenience to make a
    /// query "work": a query returning nothing under the filter is telling you
    /// the data belongs to another tenant.
    /// </para>
    /// </summary>
    IDisposable EnterPlatformScope(string reason);
}
