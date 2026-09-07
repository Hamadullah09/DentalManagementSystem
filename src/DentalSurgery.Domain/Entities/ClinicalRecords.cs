using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>A clinical entry in the patient journal. Signed notes become immutable.</summary>
public class ClinicalNote : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public Guid? ProcedureId { get; set; }
    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }

    public DateTime NoteDateUtc { get; set; } = DateTime.UtcNow;
    public ClinicalNoteType NoteType { get; set; } = ClinicalNoteType.Soap;
    public string? Title { get; set; }

    // SOAP structure
    public string? ChiefComplaint { get; set; }
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }

    /// <summary>Free-form body, used for note types that are not SOAP structured.</summary>
    public string? Body { get; set; }

    public string? ExaminationFindings { get; set; }
    public string? SoftTissueExam { get; set; }
    public bool OralCancerScreeningDone { get; set; }
    public string? OcclusionNotes { get; set; }
    public string? RadiographicFindings { get; set; }
    public string? TreatmentProvided { get; set; }
    public string? MedicationsGiven { get; set; }
    public string? PatientInstructions { get; set; }
    public string? NextVisitPlan { get; set; }

    public bool IsSigned { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public string? SignedBy { get; set; }
    public string? SignatureHash { get; set; }
    public bool IsAmended { get; set; }

    public ICollection<ClinicalNoteAddendum> Addenda { get; set; } = new List<ClinicalNoteAddendum>();

    /// <summary>Signed notes may not be edited; corrections are added as addenda.</summary>
    public bool IsEditable => !IsSigned;

    public string Summary
    {
        get
        {
            var text = Assessment ?? Body ?? Subjective ?? ChiefComplaint ?? TreatmentProvided ?? string.Empty;
            text = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return text.Length <= 140 ? text : text[..137] + "...";
        }
    }
}

/// <summary>A correction or addition appended to an already-signed note.</summary>
public class ClinicalNoteAddendum : BaseEntity
{
    public Guid ClinicalNoteId { get; set; }
    public ClinicalNote? ClinicalNote { get; set; }

    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    public string? AddedBy { get; set; }
    public Guid? AuthorStaffId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>A coded diagnosis attached to the patient, optionally tooth-specific.</summary>
public class PatientDiagnosis : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string Code { get; set; } = string.Empty;
    public string CodeSystem { get; set; } = "ICD-10";
    public string Description { get; set; } = string.Empty;

    public Guid? ToothId { get; set; }
    public Tooth? Tooth { get; set; }
    public Quadrant Quadrant { get; set; } = Quadrant.None;

    public DateOnly DiagnosedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ResolvedOn { get; set; }
    public ConditionStatus Status { get; set; } = ConditionStatus.Active;
    public TreatmentPriority Priority { get; set; } = TreatmentPriority.Routine;

    public Guid? DiagnosedByStaffId { get; set; }
    public Staff? DiagnosedByStaff { get; set; }
    public Guid? ClinicalNoteId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Dispensing location a prescription can be sent to.</summary>
public class Pharmacy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public ContactDetails Contact { get; set; } = new();
    public string? FaxNumber { get; set; }
    public string? PharmacyCode { get; set; }
    public bool AcceptsElectronicPrescriptions { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

/// <summary>A prescription issued by a clinician.</summary>
public class Prescription : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string PrescriptionNumber { get; set; } = string.Empty;
    public Guid? PrescriberStaffId { get; set; }
    public Staff? PrescriberStaff { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ProcedureId { get; set; }

    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ValidUntil { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

    public Guid? PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }

    public bool AllergiesChecked { get; set; }
    public bool InteractionsChecked { get; set; }
    public string? InteractionWarnings { get; set; }
    public string? Indication { get; set; }
    public string? Notes { get; set; }

    public bool IsSigned { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public string? SignatureData { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }

    public Guid? MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public string? FreeTextMedication { get; set; }

    public string Dosage { get; set; } = string.Empty;
    public MedicationRoute Route { get; set; } = MedicationRoute.Oral;
    public string Frequency { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public int Repeats { get; set; }
    public bool AsRequired { get; set; }
    public string? Instructions { get; set; }
    public int Sequence { get; set; }

    public string DisplayName => Medication?.Name ?? FreeTextMedication ?? "Medication";

    public string Sig =>
        string.Join(", ", new[]
        {
            Dosage,
            Frequency,
            AsRequired ? "as required" : null,
            DurationDays.HasValue ? $"for {DurationDays} days" : null
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>Metadata for a radiographic or photographic image.</summary>
public class RadiographRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public RadiographType RadiographType { get; set; } = RadiographType.Bitewing;
    public DateTime TakenAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? TakenByStaffId { get; set; }
    public Staff? TakenByStaff { get; set; }
    public Guid? AppointmentId { get; set; }

    /// <summary>Comma-separated FDI numbers covered by the image.</summary>
    public string? ToothNumbers { get; set; }
    public string? Region { get; set; }
    public DentalArch? Arch { get; set; }
    public Quadrant Quadrant { get; set; } = Quadrant.None;

    public decimal? KiloVoltagePeak { get; set; }
    public decimal? MilliAmperage { get; set; }
    public decimal? ExposureSeconds { get; set; }
    public decimal? DoseMicroSieverts { get; set; }
    public string? EquipmentUsed { get; set; }
    public bool LeadApronUsed { get; set; }
    public string? JustificationReason { get; set; }
    public bool IsRepeat { get; set; }
    public string? RepeatReason { get; set; }

    public string? Findings { get; set; }
    public string? Report { get; set; }
    public bool IsReported { get; set; }
    public Guid? ReportedByStaffId { get; set; }
    public string? QualityRating { get; set; }

    public Guid? DocumentId { get; set; }
    public PatientDocument? Document { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }
}

/// <summary>Template text for a consent form.</summary>
public class ConsentFormTemplate : LookupEntity
{
    public string Body { get; set; } = string.Empty;
    public string? Risks { get; set; }
    public string? Benefits { get; set; }
    public string? Alternatives { get; set; }
    public string Version { get; set; } = "1.0";
    public bool RequiresWitness { get; set; }
    public int? ValidForDays { get; set; }
    public string? AppliesToProcedureCodes { get; set; }
}

/// <summary>A signed instance of a consent form.</summary>
public class PatientConsent : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid ConsentFormTemplateId { get; set; }
    public ConsentFormTemplate? ConsentFormTemplate { get; set; }

    public Guid? ProcedureId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public Guid? AppointmentId { get; set; }

    public ConsentStatus Status { get; set; } = ConsentStatus.Pending;
    public DateTime? SignedAtUtc { get; set; }
    public string? SignedByName { get; set; }
    public string? SignatureData { get; set; }
    public string? RelationshipIfNotPatient { get; set; }

    public Guid? WitnessStaffId { get; set; }
    public Staff? WitnessStaff { get; set; }
    public Guid? ClinicianStaffId { get; set; }
    public Staff? ClinicianStaff { get; set; }

    public string? CapturedBodySnapshot { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public DateTime? WithdrawnAtUtc { get; set; }
    public string? WithdrawalReason { get; set; }
    public Guid? DocumentId { get; set; }
    public string? Notes { get; set; }

    public bool IsValid =>
        Status == ConsentStatus.Signed &&
        (ExpiresOn is null || ExpiresOn >= DateOnly.FromDateTime(DateTime.Today));
}

/// <summary>An inbound or outbound referral.</summary>
public class Referral : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string ReferralNumber { get; set; } = string.Empty;
    public ReferralDirection Direction { get; set; } = ReferralDirection.Outbound;
    public ReferralStatus Status { get; set; } = ReferralStatus.Draft;

    public Guid? InternalProviderId { get; set; }
    public Staff? InternalProvider { get; set; }

    public string? ExternalProviderName { get; set; }
    public string? ExternalPracticeName { get; set; }
    public string? ExternalSpecialty { get; set; }
    public ContactDetails ExternalContact { get; set; } = new();
    public Address? ExternalAddress { get; set; }

    public DateOnly ReferralDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? AppointmentDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public TreatmentPriority Priority { get; set; } = TreatmentPriority.Routine;

    public string Reason { get; set; } = string.Empty;
    public string? ClinicalSummary { get; set; }
    public string? RelevantHistory { get; set; }
    public string? ToothNumbers { get; set; }
    public bool RadiographsAttached { get; set; }
    public string? Outcome { get; set; }
    public string? Notes { get; set; }
}
