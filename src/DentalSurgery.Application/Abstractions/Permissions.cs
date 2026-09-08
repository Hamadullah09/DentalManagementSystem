namespace DentalSurgery.Application.Abstractions;

/// <summary>
/// The complete catalogue of things a user may be permitted to do.
/// <para>
/// Authorisation is expressed as <c>User → Role → Permission → Resource/Action</c>.
/// A role is only ever a bundle of these strings; nothing in the application
/// branches on a role name to decide whether an operation is allowed. That
/// indirection is what makes the permission set editable at runtime without
/// recompiling, and what keeps a new role from silently inheriting access it
/// was never granted.
/// </para>
/// <para>
/// Every constant is <c>Module.Action</c>. The module prefix is what the
/// administration screen groups by, so it must stay stable.
/// </para>
/// </summary>
public static class Permissions
{
    // ---------------------------------------------------------------- patients
    public const string PatientsView = "Patients.View";
    public const string PatientsCreate = "Patients.Create";
    public const string PatientsEdit = "Patients.Edit";
    public const string PatientsDelete = "Patients.Delete";

    /// <summary>Read the medical history, allergies and risk assessment.</summary>
    public const string MedicalHistoryView = "MedicalHistory.View";
    public const string MedicalHistoryEdit = "MedicalHistory.Edit";

    // ---------------------------------------------------------------- clinical
    public const string ClinicalRecordsView = "ClinicalRecords.View";
    public const string ClinicalRecordsCreate = "ClinicalRecords.Create";
    public const string ClinicalRecordsEdit = "ClinicalRecords.Edit";
    public const string ClinicalRecordsSign = "ClinicalRecords.Sign";

    public const string DentalChartView = "DentalChart.View";
    public const string DentalChartEdit = "DentalChart.Edit";

    public const string PeriodontalView = "Periodontal.View";
    public const string PeriodontalEdit = "Periodontal.Edit";

    public const string ProceduresView = "Procedures.View";
    public const string ProceduresRecord = "Procedures.Record";

    public const string PrescriptionsView = "Prescriptions.View";
    public const string PrescriptionsCreate = "Prescriptions.Create";

    public const string ImagingView = "Imaging.View";
    public const string ImagingCreate = "Imaging.Create";

    // ---------------------------------------------------------------- surgery
    public const string SurgeryView = "Surgery.View";
    public const string SurgeryCreate = "Surgery.Create";
    public const string SurgeryEdit = "Surgery.Edit";

    public const string ImplantsView = "Implants.View";
    public const string ImplantsRecord = "Implants.Record";

    // ---------------------------------------------------------------- scheduling
    public const string AppointmentsView = "Appointments.View";
    public const string AppointmentsCreate = "Appointments.Create";
    public const string AppointmentsEdit = "Appointments.Edit";
    public const string AppointmentsCancel = "Appointments.Cancel";

    public const string WaitingRoomView = "WaitingRoom.View";
    public const string WaitingRoomManage = "WaitingRoom.Manage";

    public const string RecallsView = "Recalls.View";
    public const string RecallsManage = "Recalls.Manage";

    // ---------------------------------------------------------------- planning
    public const string TreatmentPlansView = "TreatmentPlans.View";
    public const string TreatmentPlansCreate = "TreatmentPlans.Create";
    public const string TreatmentPlansEdit = "TreatmentPlans.Edit";
    public const string TreatmentPlansApprove = "TreatmentPlans.Approve";

    // ---------------------------------------------------------------- money
    public const string BillingView = "Billing.View";
    public const string BillingCreate = "Billing.Create";
    public const string BillingEdit = "Billing.Edit";
    public const string BillingRefund = "Billing.Refund";

    public const string InsuranceView = "Insurance.View";
    public const string InsuranceCreate = "Insurance.Create";
    public const string InsuranceSubmitClaim = "Insurance.SubmitClaim";

    // ---------------------------------------------------------------- operations
    public const string InventoryView = "Inventory.View";
    public const string InventoryCreate = "Inventory.Create";
    public const string InventoryAdjust = "Inventory.Adjust";

    public const string SterilizationView = "Sterilization.View";
    public const string SterilizationRecordCycle = "Sterilization.RecordCycle";

    public const string LaboratoryView = "Laboratory.View";
    public const string LaboratoryManage = "Laboratory.Manage";

    // ---------------------------------------------------------------- documents
    public const string DocumentsView = "Documents.View";
    public const string DocumentsUpload = "Documents.Upload";
    public const string DocumentsDelete = "Documents.Delete";

    // ---------------------------------------------------------------- comms
    public const string CommunicationsView = "Communications.View";
    public const string CommunicationsSend = "Communications.Send";

    // ---------------------------------------------------------------- tasks
    public const string TasksView = "Tasks.View";
    public const string TasksManage = "Tasks.Manage";

    // ---------------------------------------------------------------- reporting
    public const string ReportsView = "Reports.View";
    public const string ReportsExport = "Reports.Export";
    public const string ReportsFinancial = "Reports.Financial";

    // ---------------------------------------------------------------- admin
    public const string UsersView = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersEdit = "Users.Edit";
    public const string UsersDisable = "Users.Disable";

    public const string RolesView = "Roles.View";
    public const string RolesEdit = "Roles.Edit";

    public const string AuditView = "Audit.View";

    public const string SettingsView = "Settings.View";
    public const string SettingsEdit = "Settings.Edit";

    public const string IntegrationsView = "Integrations.View";
    public const string IntegrationsEdit = "Integrations.Edit";

    /// <summary>The module a permission belongs to, taken from its prefix.</summary>
    public static string ModuleOf(string permission)
    {
        var dot = permission.IndexOf('.');
        return dot < 0 ? permission : permission[..dot];
    }

    /// <summary>The action half of a permission, taken from its suffix.</summary>
    public static string ActionOf(string permission)
    {
        var dot = permission.IndexOf('.');
        return dot < 0 ? permission : permission[(dot + 1)..];
    }

    /// <summary>
    /// Modules in the order the administration screen presents them, so the
    /// permission grid reads clinical → operational → financial → system rather
    /// than alphabetically.
    /// </summary>
    public static readonly string[] ModuleOrder =
    [
        "Patients", "MedicalHistory", "ClinicalRecords", "DentalChart", "Periodontal",
        "Procedures", "Prescriptions", "Imaging", "Surgery", "Implants",
        "Appointments", "WaitingRoom", "Recalls", "TreatmentPlans",
        "Billing", "Insurance",
        "Inventory", "Sterilization", "Laboratory",
        "Documents", "Communications", "Tasks",
        "Reports",
        "Users", "Roles", "Audit", "Settings", "Integrations"
    ];

    /// <summary>A one-line description of each permission, shown in the admin grid.</summary>
    public static readonly IReadOnlyDictionary<string, string> Descriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PatientsView] = "Open the patient list and patient records.",
            [PatientsCreate] = "Register a new patient.",
            [PatientsEdit] = "Change patient demographics and contact details.",
            [PatientsDelete] = "Archive a patient record.",

            [MedicalHistoryView] = "Read medical history, allergies and the risk assessment.",
            [MedicalHistoryEdit] = "Record and update medical history.",

            [ClinicalRecordsView] = "Read clinical notes.",
            [ClinicalRecordsCreate] = "Write a new clinical note.",
            [ClinicalRecordsEdit] = "Amend a clinical note that has not been signed.",
            [ClinicalRecordsSign] = "Sign a clinical note, making it a finalised record.",

            [DentalChartView] = "View the odontogram and charting history.",
            [DentalChartEdit] = "Record findings and conditions on the chart.",

            [PeriodontalView] = "View periodontal charts and analysis.",
            [PeriodontalEdit] = "Record a periodontal assessment.",

            [ProceduresView] = "View completed and planned procedures.",
            [ProceduresRecord] = "Record a procedure as carried out.",

            [PrescriptionsView] = "View prescriptions issued to a patient.",
            [PrescriptionsCreate] = "Issue a prescription.",

            [ImagingView] = "View radiographs and the imaging register.",
            [ImagingCreate] = "Record a radiograph or upload an image.",

            [SurgeryView] = "View surgical records and the operating register.",
            [SurgeryCreate] = "Open a surgical record.",
            [SurgeryEdit] = "Complete and amend a surgical record.",

            [ImplantsView] = "View the implant registry.",
            [ImplantsRecord] = "Register a placed implant.",

            [AppointmentsView] = "View the appointment book.",
            [AppointmentsCreate] = "Book an appointment.",
            [AppointmentsEdit] = "Move, reschedule or amend an appointment.",
            [AppointmentsCancel] = "Cancel an appointment or mark a non-attendance.",

            [WaitingRoomView] = "See who is checked in and waiting.",
            [WaitingRoomManage] = "Check patients in and move them through the visit.",

            [RecallsView] = "View recall due lists.",
            [RecallsManage] = "Set and reschedule recall intervals.",

            [TreatmentPlansView] = "View treatment plans and their costings.",
            [TreatmentPlansCreate] = "Draft a treatment plan.",
            [TreatmentPlansEdit] = "Change the items on a treatment plan.",
            [TreatmentPlansApprove] = "Record a patient's acceptance or rejection of a plan.",

            [BillingView] = "View invoices, payments and the patient ledger.",
            [BillingCreate] = "Raise an invoice and record a payment.",
            [BillingEdit] = "Adjust, discount or write off a charge.",
            [BillingRefund] = "Refund a payment.",

            [InsuranceView] = "View policies, estimates and claims.",
            [InsuranceCreate] = "Record a policy and build a claim.",
            [InsuranceSubmitClaim] = "Submit a claim to the payer.",

            [InventoryView] = "View stock levels, lots and expiry.",
            [InventoryCreate] = "Add items, suppliers and purchase orders.",
            [InventoryAdjust] = "Receive, issue and adjust stock.",

            [SterilizationView] = "View sterilisation cycles and traceability.",
            [SterilizationRecordCycle] = "Record a cycle, its indicators and its release.",

            [LaboratoryView] = "View laboratory cases.",
            [LaboratoryManage] = "Raise and progress laboratory cases.",

            [DocumentsView] = "Open documents attached to a patient.",
            [DocumentsUpload] = "Attach a document to a patient record.",
            [DocumentsDelete] = "Remove a document from a patient record.",

            [CommunicationsView] = "View the correspondence history.",
            [CommunicationsSend] = "Send a message to a patient.",

            [TasksView] = "View the practice task list.",
            [TasksManage] = "Create, assign and complete tasks.",

            [ReportsView] = "Run operational and clinical reports.",
            [ReportsExport] = "Export report data out of the system.",
            [ReportsFinancial] = "Run reports containing revenue and debt.",

            [UsersView] = "View login accounts.",
            [UsersCreate] = "Create a login account.",
            [UsersEdit] = "Change an account's details and role.",
            [UsersDisable] = "Disable, re-enable or reset an account.",

            [RolesView] = "View roles and the permissions assigned to them.",
            [RolesEdit] = "Change which permissions a role grants.",

            [AuditView] = "Read the audit trail.",

            [SettingsView] = "View practice configuration.",
            [SettingsEdit] = "Change practice configuration and fee schedules.",

            [IntegrationsView] = "View the status of email, SMS and claim gateways.",
            [IntegrationsEdit] = "Change integration settings."
        };

    /// <summary>Every permission the application knows about, in module order.</summary>
    public static readonly string[] All = Descriptions.Keys
        .OrderBy(p => Array.IndexOf(ModuleOrder, ModuleOf(p)))
        .ThenBy(p => p, StringComparer.Ordinal)
        .ToArray();

    private static readonly HashSet<string> Known = new(All, StringComparer.Ordinal);

    /// <summary>True when the string names a permission this build defines.</summary>
    public static bool IsDefined(string permission) => Known.Contains(permission);
}
