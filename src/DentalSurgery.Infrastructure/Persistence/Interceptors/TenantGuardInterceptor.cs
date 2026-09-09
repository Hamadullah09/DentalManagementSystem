using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps the tenant on new rows and refuses any write that crosses a tenant
/// boundary.
/// <para>
/// The global query filter is a <em>read</em> control. On its own it leaves
/// three ways to write across the boundary, and this closes all three:
/// </para>
/// <list type="number">
/// <item>An insert with no tenant, which would create a row belonging to nobody.</item>
/// <item>An insert naming a different tenant, which would plant a row in someone
/// else's practice.</item>
/// <item>An update or delete of a row fetched outside the filter — through a
/// navigation, a stale tracked instance, or a deliberate
/// <c>IgnoreQueryFilters</c> — which is how a leak usually happens in practice,
/// because the read that produced the entity looked innocent.</item>
/// </list>
/// <para>
/// Reassigning <c>TenantId</c> on an existing row is refused outright. There is
/// no legitimate reason to move a clinical record between practices, and an
/// attempt to do so is either a bug or an attack.
/// </para>
/// </summary>
public class TenantGuardInterceptor(ITenantContext tenant, ILogger<TenantGuardInterceptor> logger)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enforce(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enforce(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Enforce(DbContextEventData eventData)
    {
        var context = eventData.Context;
        if (context is null) return;

        // A platform scope is entered deliberately by server-side code -
        // seeding, provisioning, a background sweep - and is already logged
        // where it starts.
        if (tenant.IsPlatformScope) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not ITenantScoped scoped) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            if (entry.State == EntityState.Added)
            {
                StampOnInsert(entry, scoped);
                continue;
            }

            GuardExistingRow(entry, scoped);
        }
    }

    private void StampOnInsert(EntityEntry entry, ITenantScoped scoped)
    {
        if (scoped.TenantId == Guid.Empty)
        {
            // Throws when there is no tenant in scope, which is the correct
            // outcome: a row with an empty tenant is invisible to everyone and
            // would sit in the table forever.
            scoped.TenantId = tenant.RequireTenantId();
            return;
        }

        var current = tenant.RequireTenantId();
        if (scoped.TenantId == current) return;

        logger.LogError(
            "Refused an insert of {Entity} into tenant {Target} from a scope belonging to {Current}.",
            entry.Entity.GetType().Name, scoped.TenantId, current);

        throw new TenantIsolationException(
            $"Cannot create a {entry.Entity.GetType().Name} in tenant {scoped.TenantId} " +
            $"from a scope belonging to tenant {current}.");
    }

    private void GuardExistingRow(EntityEntry entry, ITenantScoped scoped)
    {
        var current = tenant.RequireTenantId();
        var property = entry.Property(nameof(ITenantScoped.TenantId));

        // The value as it exists in the database, not as it sits in memory - an
        // attacker who sets TenantId on a tracked entity must not be able to
        // talk their way past the check by presenting the new value.
        var original = property.OriginalValue as Guid? ?? scoped.TenantId;

        if (original != current)
        {
            logger.LogError(
                "Refused a {State} of {Entity} owned by tenant {Owner} from a scope belonging to {Current}.",
                entry.State, entry.Entity.GetType().Name, original, current);

            throw new TenantIsolationException(
                $"Cannot {entry.State.ToString().ToLowerInvariant()} a {entry.Entity.GetType().Name} " +
                $"belonging to tenant {original} from a scope belonging to tenant {current}.");
        }

        if (entry.State == EntityState.Modified && property.IsModified && scoped.TenantId != original)
        {
            logger.LogError(
                "Refused an attempt to move {Entity} from tenant {From} to {To}.",
                entry.Entity.GetType().Name, original, scoped.TenantId);

            throw new TenantIsolationException(
                $"A {entry.Entity.GetType().Name} cannot be moved between tenants.");
        }
    }
}

/// <summary>
/// Raised when a write would have crossed a tenant boundary. Deliberately not
/// derived from the application's ordinary failure types: this is never
/// something to catch and recover from, and it should reach the logs.
/// </summary>
public class TenantIsolationException(string message) : Exception(message);
