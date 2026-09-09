using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace DentalSurgery.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit metadata, converts hard deletes into soft deletes, refreshes the
/// concurrency token, and writes an <see cref="AuditLog"/> row for every change.
/// The audit rows join the same SaveChanges call, so the trail and the change it
/// describes commit or roll back together.
/// </summary>
public class AuditingInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ITenantContext tenant) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    /// <summary>Never copied into the audit trail.</summary>
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "RowVersion",
        "SignatureData", "SignatureHash", "CapturedBodySnapshot"
    };

    /// <summary>Change-tracked but not worth an audit row.</summary>
    private static readonly HashSet<string> ExcludedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuditLog), nameof(NumberSequence)
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) Process(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) Process(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // ------------------------------------------------------------------

    private void Process(DbContext context)
    {
        var now = clock.UtcNow;
        var user = currentUser.UserName ?? "system";
        var audits = new List<AuditLog>();

        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

            StampAuditFields(entry, now, user);
            var softDeleted = ConvertDeleteToSoftDelete(entry, now, user);
            RefreshConcurrencyToken(entry);

            var audit = BuildAuditEntry(entry, softDeleted, now);
            if (audit is not null) audits.Add(audit);
        }

        if (audits.Count > 0) context.Set<AuditLog>().AddRange(audits);
    }

    private static void StampAuditFields(EntityEntry entry, DateTime now, string user)
    {
        if (entry.Entity is not IAuditable auditable) return;

        switch (entry.State)
        {
            case EntityState.Added:
                if (auditable.CreatedAtUtc == default) auditable.CreatedAtUtc = now;
                auditable.CreatedBy ??= user;
                break;
            case EntityState.Modified:
                auditable.ModifiedAtUtc = now;
                auditable.ModifiedBy = user;
                break;
        }
    }

    /// <summary>
    /// Clinical records are never physically removed: a delete becomes an update
    /// that sets the soft-delete flags.
    /// </summary>
    private static bool ConvertDeleteToSoftDelete(EntityEntry entry, DateTime now, string user)
    {
        if (entry.State != EntityState.Deleted) return false;
        if (entry.Entity is not ISoftDeletable deletable) return false;

        entry.State = EntityState.Modified;
        deletable.IsDeleted = true;
        deletable.DeletedAtUtc = now;
        deletable.DeletedBy = user;
        return true;
    }

    /// <summary>SQLite has no native rowversion, so the token is regenerated on each write.</summary>
    private static void RefreshConcurrencyToken(EntityEntry entry)
    {
        if (entry.Entity is not BaseEntity) return;
        if (entry.State is not (EntityState.Added or EntityState.Modified)) return;

        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(BaseEntity.RowVersion));
        if (property is null) return;

        // On SQL Server the column is a real rowversion, stamped by the database
        // on every write. Assigning to it would be rejected, and the value here
        // would be wrong anyway. Only the SQLite fallback needs a token written
        // by hand.
        if (property.Metadata.ValueGenerated.HasFlag(ValueGenerated.OnUpdate)) return;

        property.CurrentValue = Guid.NewGuid().ToByteArray();
    }

    private AuditLog? BuildAuditEntry(EntityEntry entry, bool wasSoftDelete, DateTime now)
    {
        var entityName = entry.Metadata.ClrType.Name;
        if (ExcludedEntities.Contains(entityName)) return null;
        if (entry.Metadata.IsOwned()) return null;

        var action = wasSoftDelete
            ? AuditAction.Delete
            : entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Deleted => AuditAction.Delete,
                _ => AuditAction.Update
            };

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changed = new List<string>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (SensitiveProperties.Contains(name)) continue;

            switch (action)
            {
                case AuditAction.Create:
                    if (property.CurrentValue is not null) newValues[name] = Stringify(property.CurrentValue);
                    break;

                case AuditAction.Delete when entry.State == EntityState.Deleted:
                    if (property.OriginalValue is not null) oldValues[name] = Stringify(property.OriginalValue);
                    break;

                default:
                    if (!property.IsModified) continue;
                    if (Equals(property.OriginalValue, property.CurrentValue)) continue;
                    changed.Add(name);
                    oldValues[name] = Stringify(property.OriginalValue);
                    newValues[name] = Stringify(property.CurrentValue);
                    break;
            }
        }

        // An update that changed nothing meaningful is not worth a row.
        if (action == AuditAction.Update && changed.Count == 0) return null;

        return new AuditLog
        {
            // The audit row inherits the tenant of the row it describes, not the
            // tenant of the scope writing it. During a platform operation these
            // differ, and the trail belongs with the data it is about.
            TenantId = entry.Entity is ITenantScoped owned && owned.TenantId != Guid.Empty
                ? owned.TenantId
                : tenant.TenantId ?? Guid.Empty,
            TimestampUtc = now,
            Action = action,
            EntityName = entityName,
            EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString(),
            UserId = currentUser.UserId,
            UserName = currentUser.UserName ?? "system",
            IpAddress = currentUser.IpAddress,
            OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            ChangedColumns = changed.Count == 0 ? null : string.Join(",", changed),
            PatientId = ResolvePatientId(entry)
        };
    }

    /// <summary>Links the audit row to a patient so a per-record trail can be shown.</summary>
    private static Guid? ResolvePatientId(EntityEntry entry)
    {
        if (entry.Entity is Patient patient) return patient.Id;
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "PatientId");
        return property?.CurrentValue as Guid?;
    }

    private static object? Stringify(object? value) => value switch
    {
        null => null,
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTime dt => dt.ToString("O"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        TimeSpan t => t.ToString(),
        Enum e => e.ToString(),
        _ => value
    };
}
