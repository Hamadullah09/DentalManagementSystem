using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>
/// Immutable trail of data changes. Written automatically by the persistence
/// layer, never edited, and required for clinical-record governance.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>JSON snapshot of changed properties before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of changed properties after the change.</summary>
    public string? NewValues { get; set; }

    public string? ChangedColumns { get; set; }

    /// <summary>Set when the change relates to a patient, so a per-patient trail can be shown.</summary>
    public Guid? PatientId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Key/value application configuration held in the database.</summary>
public class AppSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public string DataType { get; set; } = "string";
    public bool IsSystem { get; set; }
    public bool IsEncrypted { get; set; }
}

/// <summary>
/// Allocates human-readable document numbers (patients, invoices, claims).
/// Incremented under a transaction so numbers are gap-free and unique.
/// </summary>
public class NumberSequence : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public long NextValue { get; set; } = 1;
    public int PadWidth { get; set; } = 6;
    public bool IncludeYear { get; set; }
    public int? ResetYear { get; set; }

    public string Format(long value)
    {
        var number = value.ToString().PadLeft(PadWidth, '0');
        var year = IncludeYear ? $"{DateTime.UtcNow:yyyy}-" : string.Empty;
        return $"{Prefix}{year}{number}{Suffix}";
    }
}

/// <summary>A saved, named search or worklist filter.</summary>
public class SavedView : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public bool IsShared { get; set; }
    public string FilterJson { get; set; } = "{}";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>An in-app task assigned to a staff member.</summary>
public class WorkTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? AssignedToStaffId { get; set; }
    public Staff? AssignedToStaff { get; set; }
    public Guid? RelatedAppointmentId { get; set; }
    public Guid? RelatedLabCaseId { get; set; }
    public Guid? RelatedClaimId { get; set; }

    public TreatmentPriority Priority { get; set; } = TreatmentPriority.Routine;
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletedBy { get; set; }
    public string? Notes { get; set; }

    public bool IsOverdue =>
        !IsCompleted && DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.Today);
}
