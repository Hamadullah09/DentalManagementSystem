namespace DentalSurgery.Application.Abstractions;

/// <summary>
/// The permissions each role grants out of the box.
/// <para>
/// This is the seed, not the runtime source of truth. On first run each entry
/// is written to the role as a claim, and from then on an administrator can
/// change the grant without a deployment. Keeping the defaults here means a
/// fresh database, a test fixture and a restored environment all start from the
/// same, reviewable matrix.
/// </para>
/// <para>
/// The guiding rule is least privilege, and the separations below are
/// deliberate rather than incidental:
/// </para>
/// <list type="bullet">
/// <item>Reception can reach a patient without reading their clinical notes.</item>
/// <item>A hygienist works the periodontal and preventive record, and cannot prescribe or operate.</item>
/// <item>A practice manager sees the money and the operation, and holds no clinical write access at all.</item>
/// <item>Only an administrator can change what a role is allowed to do.</item>
/// </list>
/// </summary>
public static class RolePermissions
{
    // ------------------------------------------------------------ reception
    private static readonly string[] Receptionist =
    [
        Permissions.PatientsView, Permissions.PatientsCreate, Permissions.PatientsEdit,

        Permissions.AppointmentsView, Permissions.AppointmentsCreate,
        Permissions.AppointmentsEdit, Permissions.AppointmentsCancel,

        Permissions.WaitingRoomView, Permissions.WaitingRoomManage,
        Permissions.RecallsView, Permissions.RecallsManage,

        // The front desk quotes and takes money, but does not adjust or refund it.
        Permissions.TreatmentPlansView,
        Permissions.BillingView, Permissions.BillingCreate,
        Permissions.InsuranceView,

        Permissions.DocumentsView, Permissions.DocumentsUpload,
        Permissions.CommunicationsView, Permissions.CommunicationsSend,
        Permissions.TasksView, Permissions.TasksManage,
        Permissions.LaboratoryView
    ];

    // ------------------------------------------------------------ dentist
    private static readonly string[] Dentist =
    [
        Permissions.PatientsView, Permissions.PatientsCreate, Permissions.PatientsEdit,
        Permissions.MedicalHistoryView, Permissions.MedicalHistoryEdit,

        Permissions.ClinicalRecordsView, Permissions.ClinicalRecordsCreate,
        Permissions.ClinicalRecordsEdit, Permissions.ClinicalRecordsSign,

        Permissions.DentalChartView, Permissions.DentalChartEdit,
        Permissions.PeriodontalView, Permissions.PeriodontalEdit,
        Permissions.ProceduresView, Permissions.ProceduresRecord,
        Permissions.PrescriptionsView, Permissions.PrescriptionsCreate,
        Permissions.ImagingView, Permissions.ImagingCreate,

        // General practice covers minor oral surgery; placing implants does not
        // follow from it, so that stays with the surgeon.
        Permissions.SurgeryView, Permissions.SurgeryCreate, Permissions.SurgeryEdit,
        Permissions.ImplantsView,

        Permissions.AppointmentsView, Permissions.AppointmentsCreate,
        Permissions.AppointmentsEdit, Permissions.AppointmentsCancel,
        Permissions.WaitingRoomView, Permissions.WaitingRoomManage,
        Permissions.RecallsView, Permissions.RecallsManage,

        Permissions.TreatmentPlansView, Permissions.TreatmentPlansCreate,
        Permissions.TreatmentPlansEdit, Permissions.TreatmentPlansApprove,

        // Charges for treatment carried out; corrections belong to the office.
        Permissions.BillingView, Permissions.BillingCreate,
        Permissions.InsuranceView,

        Permissions.DocumentsView, Permissions.DocumentsUpload,
        Permissions.CommunicationsView, Permissions.CommunicationsSend,
        Permissions.TasksView, Permissions.TasksManage,

        Permissions.LaboratoryView, Permissions.LaboratoryManage,
        Permissions.SterilizationView, Permissions.InventoryView,

        Permissions.ReportsView
    ];

    // ------------------------------------------------------------ oral surgeon
    private static readonly string[] OralSurgeon =
    [
        .. Dentist,
        Permissions.ImplantsRecord
    ];

    // ------------------------------------------------------------ hygienist
    private static readonly string[] Hygienist =
    [
        Permissions.PatientsView,
        Permissions.MedicalHistoryView, Permissions.MedicalHistoryEdit,

        Permissions.ClinicalRecordsView, Permissions.ClinicalRecordsCreate,
        Permissions.ClinicalRecordsEdit, Permissions.ClinicalRecordsSign,

        Permissions.DentalChartView, Permissions.DentalChartEdit,
        Permissions.PeriodontalView, Permissions.PeriodontalEdit,
        Permissions.ProceduresView, Permissions.ProceduresRecord,

        // Reads radiographs, does not expose or prescribe.
        Permissions.ImagingView,
        Permissions.PrescriptionsView,

        Permissions.AppointmentsView, Permissions.AppointmentsCreate, Permissions.AppointmentsEdit,
        Permissions.WaitingRoomView, Permissions.WaitingRoomManage,
        Permissions.RecallsView, Permissions.RecallsManage,

        Permissions.TreatmentPlansView, Permissions.TreatmentPlansCreate,

        Permissions.DocumentsView, Permissions.DocumentsUpload,
        Permissions.CommunicationsView,
        Permissions.TasksView, Permissions.TasksManage,

        Permissions.SterilizationView, Permissions.SterilizationRecordCycle,
        Permissions.InventoryView,

        Permissions.BillingView
    ];

    // ------------------------------------------------------------ practice manager
    private static readonly string[] PracticeManager =
    [
        Permissions.PatientsView, Permissions.PatientsCreate, Permissions.PatientsEdit,

        Permissions.AppointmentsView, Permissions.AppointmentsCreate,
        Permissions.AppointmentsEdit, Permissions.AppointmentsCancel,
        Permissions.WaitingRoomView, Permissions.WaitingRoomManage,
        Permissions.RecallsView, Permissions.RecallsManage,

        // Sees what a plan costs and whether it was accepted, not what was
        // diagnosed. No clinical read or write appears anywhere in this list.
        Permissions.TreatmentPlansView,

        Permissions.BillingView, Permissions.BillingCreate,
        Permissions.BillingEdit, Permissions.BillingRefund,
        Permissions.InsuranceView, Permissions.InsuranceCreate, Permissions.InsuranceSubmitClaim,

        Permissions.InventoryView, Permissions.InventoryCreate, Permissions.InventoryAdjust,
        Permissions.SterilizationView,
        Permissions.LaboratoryView, Permissions.LaboratoryManage,

        Permissions.DocumentsView, Permissions.DocumentsUpload,
        Permissions.CommunicationsView, Permissions.CommunicationsSend,
        Permissions.TasksView, Permissions.TasksManage,

        Permissions.ReportsView, Permissions.ReportsExport, Permissions.ReportsFinancial,

        // Runs the staff list; cannot widen what any role is allowed to do.
        Permissions.UsersView, Permissions.UsersCreate,
        Permissions.UsersEdit, Permissions.UsersDisable,
        Permissions.SettingsView, Permissions.IntegrationsView
    ];

    // ------------------------------------------------------------ nurse
    private static readonly string[] Nurse =
    [
        Permissions.PatientsView,
        Permissions.MedicalHistoryView,
        Permissions.ClinicalRecordsView,
        Permissions.DentalChartView,
        Permissions.PeriodontalView,
        Permissions.ProceduresView,
        Permissions.ImagingView, Permissions.ImagingCreate,
        Permissions.SurgeryView,
        Permissions.ImplantsView,

        Permissions.AppointmentsView,
        Permissions.WaitingRoomView, Permissions.WaitingRoomManage,

        Permissions.DocumentsView, Permissions.DocumentsUpload,
        Permissions.TasksView, Permissions.TasksManage,

        Permissions.InventoryView, Permissions.InventoryCreate, Permissions.InventoryAdjust,
        Permissions.SterilizationView, Permissions.SterilizationRecordCycle,
        Permissions.LaboratoryView, Permissions.LaboratoryManage
    ];

    // ------------------------------------------------------------ accounts
    private static readonly string[] Accounts =
    [
        Permissions.PatientsView,
        Permissions.AppointmentsView,
        Permissions.TreatmentPlansView,

        Permissions.BillingView, Permissions.BillingCreate,
        Permissions.BillingEdit, Permissions.BillingRefund,
        Permissions.InsuranceView, Permissions.InsuranceCreate, Permissions.InsuranceSubmitClaim,

        Permissions.DocumentsView,
        Permissions.CommunicationsView, Permissions.CommunicationsSend,
        Permissions.TasksView, Permissions.TasksManage,

        Permissions.ReportsView, Permissions.ReportsExport, Permissions.ReportsFinancial
    ];

    // ------------------------------------------------------------ read only
    private static readonly string[] ReadOnly =
    [
        Permissions.PatientsView,
        Permissions.AppointmentsView,
        Permissions.WaitingRoomView,
        Permissions.RecallsView,
        Permissions.TreatmentPlansView,
        Permissions.TasksView
    ];

    /// <summary>The default grant for each role, keyed by role name.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Defaults =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // The administrator is defined as "everything this build knows about",
            // so a permission added in a later release is granted automatically
            // rather than leaving the system with no one able to use it.
            [Roles.Administrator] = Permissions.All,
            [Roles.PracticeManager] = PracticeManager,
            [Roles.Dentist] = Dentist,
            [Roles.OralSurgeon] = OralSurgeon,
            [Roles.Hygienist] = Hygienist,
            [Roles.Nurse] = Nurse,
            [Roles.Receptionist] = Receptionist,
            [Roles.Accounts] = Accounts,
            [Roles.ReadOnly] = ReadOnly
        };

    /// <summary>The default grant for a role, or an empty set for one we do not know.</summary>
    public static string[] For(string role) =>
        Defaults.TryGetValue(role, out var permissions) ? permissions : [];

    /// <summary>
    /// The union of the defaults for a set of roles. Used when seeding and in
    /// tests; at runtime the grant is read from the database.
    /// </summary>
    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles) set.UnionWith(For(role));
        return set;
    }
}
