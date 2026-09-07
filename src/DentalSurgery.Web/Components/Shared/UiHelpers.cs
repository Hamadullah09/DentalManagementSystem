using DentalSurgery.Application.Clinical;
using DentalSurgery.Domain.Enums;
using System.Text.RegularExpressions;

namespace DentalSurgery.Web.Components.Shared;

/// <summary>Formatting and colour mapping shared by the screens.</summary>
public static class Ui
{
    public const string Currency = "£";

    public static string Money(decimal value) => $"{Currency}{value:N2}";

    public static string MoneyCompact(decimal value) => value switch
    {
        >= 1_000_000 or <= -1_000_000 => $"{Currency}{value / 1_000_000m:0.##}M",
        >= 10_000 or <= -10_000 => $"{Currency}{value / 1_000m:0.#}k",
        _ => $"{Currency}{value:N0}"
    };

    public static string Date(DateOnly? value) => value?.ToString("d MMM yyyy") ?? "-";
    public static string DateShort(DateOnly? value) => value?.ToString("dd/MM/yy") ?? "-";
    public static string DateTimeLocal(DateTime? value) => value?.ToString("d MMM yyyy HH:mm") ?? "-";
    public static string Time(DateTime? value) => value?.ToString("HH:mm") ?? "-";

    /// <summary>"3 days ago", "in 2 weeks", and so on.</summary>
    public static string Relative(DateOnly? value, DateOnly? today = null)
    {
        if (value is null) return "-";
        var reference = today ?? DateOnly.FromDateTime(DateTime.Today);
        var days = value.Value.DayNumber - reference.DayNumber;

        return days switch
        {
            0 => "today",
            1 => "tomorrow",
            -1 => "yesterday",
            > 0 and < 7 => $"in {days} days",
            < 0 and > -7 => $"{-days} days ago",
            > 0 and < 60 => $"in {days / 7} week{(days / 7 == 1 ? "" : "s")}",
            < 0 and > -60 => $"{-days / 7} week{(-days / 7 == 1 ? "" : "s")} ago",
            > 0 => $"in {days / 30} month{(days / 30 == 1 ? "" : "s")}",
            _ => $"{-days / 30} month{(-days / 30 == 1 ? "" : "s")} ago"
        };
    }

    /// <summary>Turns a PascalCase enum name into readable words.</summary>
    public static string Humanise(string? pascalCase) =>
        string.IsNullOrEmpty(pascalCase)
            ? string.Empty
            : Regex.Replace(pascalCase, "(?<!^)([A-Z])", " $1").Replace("  ", " ");

    public static string Humanise(Enum? value) => value is null ? "-" : Humanise(value.ToString());

    public static string Truncate(string? text, int length)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var clean = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return clean.Length <= length ? clean : clean[..(length - 1)] + "…";
    }

    public static string Initials(string? first, string? last)
    {
        var f = string.IsNullOrWhiteSpace(first) ? "" : first[0].ToString();
        var l = string.IsNullOrWhiteSpace(last) ? "" : last[0].ToString();
        var result = (f + l).ToUpperInvariant();
        return string.IsNullOrEmpty(result) ? "?" : result;
    }

    // ---------------------------------------------------------------- badges

    public static string BadgeFor(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Unconfirmed => "b-neutral",
        AppointmentStatus.Confirmed => "b-info",
        AppointmentStatus.ArrivedWaiting => "b-warning",
        AppointmentStatus.Seated or AppointmentStatus.InTreatment => "b-purple",
        AppointmentStatus.Completed or AppointmentStatus.CheckedOut => "b-success",
        AppointmentStatus.Cancelled or AppointmentStatus.Broken => "b-danger",
        AppointmentStatus.NoShow => "b-danger",
        _ => "b-neutral"
    };

    public static string SlotClassFor(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Unconfirmed => "s-unconfirmed",
        AppointmentStatus.Confirmed => "s-confirmed",
        AppointmentStatus.ArrivedWaiting => "s-waiting",
        AppointmentStatus.Seated or AppointmentStatus.InTreatment => "s-seated",
        AppointmentStatus.Completed or AppointmentStatus.CheckedOut => "s-complete",
        _ => "s-cancelled"
    };

    public static string BadgeFor(PatientStatus status) => status switch
    {
        PatientStatus.Active => "b-success",
        PatientStatus.Prospective => "b-info",
        PatientStatus.Inactive => "b-neutral",
        PatientStatus.Archived or PatientStatus.Transferred => "b-neutral",
        PatientStatus.Deceased => "b-danger",
        _ => "b-neutral"
    };

    public static string BadgeFor(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "b-neutral",
        InvoiceStatus.Issued => "b-info",
        InvoiceStatus.PartiallyPaid => "b-warning",
        InvoiceStatus.Paid => "b-success",
        InvoiceStatus.Overdue => "b-danger",
        InvoiceStatus.Void or InvoiceStatus.WrittenOff => "b-neutral",
        InvoiceStatus.Refunded => "b-purple",
        _ => "b-neutral"
    };

    public static string BadgeFor(TreatmentPlanStatus status) => status switch
    {
        TreatmentPlanStatus.Draft => "b-neutral",
        TreatmentPlanStatus.Presented => "b-info",
        TreatmentPlanStatus.Accepted => "b-success",
        TreatmentPlanStatus.PartiallyAccepted => "b-warning",
        TreatmentPlanStatus.InProgress => "b-purple",
        TreatmentPlanStatus.Completed => "b-success",
        TreatmentPlanStatus.Declined => "b-danger",
        _ => "b-neutral"
    };

    public static string BadgeFor(TreatmentPlanItemStatus status) => status switch
    {
        TreatmentPlanItemStatus.Proposed => "b-neutral",
        TreatmentPlanItemStatus.Accepted => "b-info",
        TreatmentPlanItemStatus.Scheduled => "b-purple",
        TreatmentPlanItemStatus.InProgress => "b-warning",
        TreatmentPlanItemStatus.Completed => "b-success",
        TreatmentPlanItemStatus.Declined or TreatmentPlanItemStatus.Removed => "b-danger",
        _ => "b-neutral"
    };

    public static string BadgeFor(ClaimStatus status) => status switch
    {
        ClaimStatus.Draft or ClaimStatus.ReadyToSend => "b-neutral",
        ClaimStatus.Submitted or ClaimStatus.Received or ClaimStatus.InReview => "b-info",
        ClaimStatus.Approved or ClaimStatus.Paid => "b-success",
        ClaimStatus.PartiallyApproved => "b-warning",
        ClaimStatus.Denied => "b-danger",
        ClaimStatus.Appealed => "b-purple",
        _ => "b-neutral"
    };

    public static string BadgeFor(LabCaseStatus status) => status switch
    {
        LabCaseStatus.Draft => "b-neutral",
        LabCaseStatus.Sent or LabCaseStatus.InProduction or LabCaseStatus.Shipped => "b-info",
        LabCaseStatus.Received or LabCaseStatus.TryIn => "b-warning",
        LabCaseStatus.Delivered => "b-success",
        LabCaseStatus.Remake => "b-danger",
        LabCaseStatus.Cancelled => "b-neutral",
        _ => "b-neutral"
    };

    public static string BadgeFor(SterilisationResult result) => result switch
    {
        SterilisationResult.Pass => "b-success",
        SterilisationResult.Fail => "b-danger",
        SterilisationResult.Quarantined => "b-warning",
        _ => "b-neutral"
    };

    public static string BadgeFor(AlertSeverity severity) => severity switch
    {
        AlertSeverity.Critical => "b-danger",
        AlertSeverity.High => "b-danger",
        AlertSeverity.Medium => "b-warning",
        AlertSeverity.Low => "b-info",
        _ => "b-neutral"
    };

    public static string BadgeFor(AllergySeverity severity) => severity switch
    {
        AllergySeverity.Anaphylaxis or AllergySeverity.Severe => "b-danger",
        AllergySeverity.Moderate => "b-warning",
        AllergySeverity.Mild => "b-info",
        _ => "b-neutral"
    };

    public static string BadgeFor(RecallStatus status) => status switch
    {
        RecallStatus.Booked => "b-success",
        RecallStatus.Overdue => "b-danger",
        RecallStatus.Due => "b-warning",
        RecallStatus.Scheduled => "b-info",
        _ => "b-neutral"
    };

    public static string BadgeFor(ImplantStatus status) => status switch
    {
        ImplantStatus.Restored => "b-success",
        ImplantStatus.Placed or ImplantStatus.Integrating or ImplantStatus.Exposed => "b-info",
        ImplantStatus.Failing => "b-warning",
        ImplantStatus.Failed or ImplantStatus.Explanted => "b-danger",
        _ => "b-neutral"
    };

    public static string BadgeFor(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Received => "b-success",
        PurchaseOrderStatus.PartiallyReceived => "b-warning",
        PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Acknowledged => "b-info",
        PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Disputed => "b-danger",
        _ => "b-neutral"
    };

    public static string BadgeFor(TreatmentPriority priority) => priority switch
    {
        TreatmentPriority.Emergency => "b-danger",
        TreatmentPriority.Urgent => "b-danger",
        TreatmentPriority.High => "b-warning",
        TreatmentPriority.Routine => "b-info",
        _ => "b-neutral"
    };

    public static string BadgeFor(PrescriptionStatus status) => status switch
    {
        PrescriptionStatus.Issued or PrescriptionStatus.Transmitted => "b-info",
        PrescriptionStatus.Dispensed => "b-success",
        PrescriptionStatus.Cancelled or PrescriptionStatus.Expired => "b-neutral",
        _ => "b-neutral"
    };

    public static string BadgeFor(ProcedureStatus status) => status switch
    {
        ProcedureStatus.Completed => "b-success",
        ProcedureStatus.InProgress => "b-warning",
        ProcedureStatus.Scheduled or ProcedureStatus.Planned => "b-info",
        ProcedureStatus.Cancelled or ProcedureStatus.Voided => "b-danger",
        _ => "b-neutral"
    };

    // ---------------------------------------------------------------- colours

    public static string ColourFor(ProcedureCategory category) => category switch
    {
        ProcedureCategory.Diagnostic or ProcedureCategory.Radiology => "#1266d6",
        ProcedureCategory.Preventive => "#0aa2a2",
        ProcedureCategory.Restorative => "#1c7c4a",
        ProcedureCategory.Endodontics => "#c77700",
        ProcedureCategory.Periodontics => "#6a3fbf",
        ProcedureCategory.OralAndMaxillofacialSurgery => "#d13438",
        ProcedureCategory.ImplantServices => "#8a4fbf",
        ProcedureCategory.ProsthodonticsFixed or ProcedureCategory.ProsthodonticsRemovable => "#0d7a8f",
        ProcedureCategory.Orthodontics => "#b5359c",
        ProcedureCategory.Cosmetic => "#d1668b",
        _ => "#5f6b7a"
    };

    public static string RiskColour(RiskLevel level) => level switch
    {
        RiskLevel.Critical => "var(--ds-danger)",
        RiskLevel.High => "var(--ds-danger)",
        RiskLevel.Moderate => "var(--ds-warning)",
        RiskLevel.Low => "var(--ds-info)",
        _ => "var(--ds-text-muted)"
    };

    public static string PocketClass(int depth) => depth switch
    {
        >= 6 => "pd-deep",
        >= 4 => "pd-moderate",
        _ => "pd-shallow"
    };
}
