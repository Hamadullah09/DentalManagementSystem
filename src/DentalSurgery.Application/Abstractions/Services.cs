using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Application.Abstractions;

/// <summary>Identity of the signed-in operator, used for auditing and permissions.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    Guid? StaffId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
    string? IpAddress { get; }
}

/// <summary>Clock abstraction so time-dependent logic can be tested deterministically.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
    DateOnly Today { get; }
}

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>Stores and retrieves patient documents and images.</summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string subFolder, CancellationToken ct = default);
    Task<Stream?> OpenAsync(string storagePath, CancellationToken ct = default);
    Task<bool> DeleteAsync(string storagePath, CancellationToken ct = default);
    bool Exists(string storagePath);
    string GetPublicUrl(string storagePath);
}

/// <summary>The outcome of an attempt to deliver a patient communication.</summary>
public record NotificationResult(
    bool Delivered,
    string Provider,
    string? ProviderMessageId = null,
    string? Error = null)
{
    /// <summary>The channel had no gateway configured, so the message was only recorded.</summary>
    public bool WasRecordedOnly => Provider == "recorded";

    public static NotificationResult Sent(string provider, string? messageId = null) =>
        new(true, provider, messageId);

    public static NotificationResult Failed(string provider, string error) =>
        new(false, provider, null, error);

    public static NotificationResult Recorded() =>
        new(false, "recorded", null, "No gateway is configured for this channel.");
}

/// <summary>Sends patient communications through whichever gateways are configured.</summary>
public interface INotificationSender
{
    /// <summary>True when the channel has a working gateway behind it.</summary>
    bool IsChannelConfigured(CommunicationChannel channel);

    Task<NotificationResult> SendAsync(
        CommunicationChannel channel,
        string recipient,
        string? subject,
        string body,
        CancellationToken ct = default);
}

/// <summary>Transmits a generated insurance claim to a payer or clearing house.</summary>
public interface IClaimSubmissionGateway
{
    string Name { get; }
    bool IsConfigured { get; }

    Task<ClaimSubmissionResult> SubmitAsync(
        string claimNumber,
        string interchange,
        CancellationToken ct = default);
}

public record ClaimSubmissionResult(
    bool Accepted,
    string Method,
    string? Reference = null,
    string? Error = null,
    string? StoredAtPath = null)
{
    public static ClaimSubmissionResult Success(string method, string reference, string? path = null) =>
        new(true, method, reference, null, path);

    public static ClaimSubmissionResult Failure(string method, string error) =>
        new(false, method, null, error);
}

/// <summary>Allocates gap-free human-readable document numbers.</summary>
public interface INumberSequenceService
{
    Task<string> NextAsync(string sequenceName, CancellationToken ct = default);
}

/// <summary>Well-known sequence names used across the application.</summary>
public static class SequenceNames
{
    public const string Patient = "Patient";
    public const string Appointment = "Appointment";
    public const string Invoice = "Invoice";
    public const string Payment = "Payment";
    public const string TreatmentPlan = "TreatmentPlan";
    public const string Claim = "Claim";
    public const string Prescription = "Prescription";
    public const string LabCase = "LabCase";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string Referral = "Referral";
    public const string Staff = "Staff";
    public const string PaymentPlan = "PaymentPlan";
}

/// <summary>Application role names. Kept in one place so policies and seeding agree.</summary>
public static class Roles
{
    public const string Administrator = "Administrator";
    public const string PracticeManager = "PracticeManager";
    public const string Dentist = "Dentist";
    public const string OralSurgeon = "OralSurgeon";
    public const string Hygienist = "Hygienist";
    public const string Nurse = "Nurse";
    public const string Receptionist = "Receptionist";
    public const string Accounts = "Accounts";
    public const string ReadOnly = "ReadOnly";

    public static readonly string[] All =
    {
        Administrator, PracticeManager, Dentist, OralSurgeon, Hygienist, Nurse,
        Receptionist, Accounts, ReadOnly
    };

    /// <summary>
    /// The six roles a dental hospital is organised around. Each has its own
    /// home dashboard and its own default permission grant.
    /// </summary>
    public static readonly string[] Primary =
    {
        Administrator, PracticeManager, Dentist, OralSurgeon, Hygienist, Receptionist
    };

    /// <summary>Roles permitted to write clinical records.</summary>
    public static readonly string[] Clinical = { Administrator, Dentist, OralSurgeon, Hygienist, Nurse };

    /// <summary>Roles permitted to change money.</summary>
    public static readonly string[] Financial = { Administrator, PracticeManager, Accounts, Receptionist };

    /// <summary>The label shown in the interface for a role name.</summary>
    public static string Display(string role) => role switch
    {
        PracticeManager => "Practice manager",
        OralSurgeon => "Oral surgeon",
        ReadOnly => "Read only",
        _ => role
    };
}

/// <summary>
/// Authorisation policy names.
/// <para>
/// Every policy here is a permission name from <see cref="Permissions"/>. The
/// policy provider builds a requirement for whatever string it is handed, so
/// <c>[Authorize(Policy = Permissions.PatientsView)]</c> works without anything
/// being registered up front. The aliases below only exist so that a page
/// guarding a whole module reads as one idea rather than a list.
/// </para>
/// </summary>
public static class Policies
{
    /// <summary>Signed in, active, and holding at least one permission.</summary>
    public const string StaffMember = "StaffMember";

    public const string CanViewClinical = Permissions.ClinicalRecordsView;
    public const string CanEditClinical = Permissions.ClinicalRecordsCreate;
    public const string CanPrescribe = Permissions.PrescriptionsCreate;
    public const string CanManageSchedule = Permissions.AppointmentsView;
    public const string CanManageBilling = Permissions.BillingView;
    public const string CanManageInventory = Permissions.InventoryView;
    public const string CanManageStaff = Permissions.UsersView;
    public const string CanViewReports = Permissions.ReportsView;
    public const string CanAdminister = Permissions.SettingsEdit;
}
