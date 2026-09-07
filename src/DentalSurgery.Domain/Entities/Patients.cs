using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>Root record for a person receiving care at the practice.</summary>
public class Patient : BaseEntity
{
    /// <summary>Human-facing sequential identifier, e.g. "P-001042".</summary>
    public string PatientNumber { get; set; } = string.Empty;

    public PersonName Name { get; set; } = new();
    public ContactDetails Contact { get; set; } = new();
    public Address Address { get; set; } = new();

    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? DateOfDeath { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Unknown;
    public string? NationalInsuranceNumber { get; set; }
    public string? NhsNumber { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? PreferredLanguage { get; set; } = "English";
    public bool RequiresInterpreter { get; set; }
    public string? Ethnicity { get; set; }
    public string? PhotoPath { get; set; }

    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public DateOnly RegistrationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? InactiveDate { get; set; }
    public string? InactiveReason { get; set; }

    public string? ReferralSource { get; set; }
    public Guid? ReferredByPatientId { get; set; }
    public Patient? ReferredByPatient { get; set; }

    public Guid? PrimaryProviderId { get; set; }
    public Staff? PrimaryProvider { get; set; }
    public Guid? PrimaryHygienistId { get; set; }
    public Staff? PrimaryHygienist { get; set; }
    public Guid? PreferredLocationId { get; set; }
    public Location? PreferredLocation { get; set; }

    // --- recall & marketing preferences -----------------------------------
    public int RecallIntervalMonths { get; set; } = 6;
    public DateOnly? LastExamDate { get; set; }
    public DateOnly? LastHygieneDate { get; set; }
    public DateOnly? LastRadiographDate { get; set; }
    public DateOnly? NextRecallDue { get; set; }

    public bool AllowEmail { get; set; } = true;
    public bool AllowSms { get; set; } = true;
    public bool AllowPhoneCall { get; set; } = true;
    public bool AllowPost { get; set; } = true;
    public bool AllowMarketing { get; set; }
    public DateTime? ConsentToContactAtUtc { get; set; }
    public DateTime? PrivacyNoticeAcceptedAtUtc { get; set; }

    // --- financial snapshot (maintained by the ledger service) -------------
    public decimal AccountBalance { get; set; }
    public decimal InsurancePending { get; set; }
    public Guid? GuarantorPatientId { get; set; }
    public Patient? GuarantorPatient { get; set; }

    public string? GeneralPractitionerName { get; set; }
    public string? GeneralPracticeSurgery { get; set; }
    public string? Notes { get; set; }

    // --- navigation --------------------------------------------------------
    public ICollection<PatientContact> Contacts { get; set; } = new List<PatientContact>();
    public ICollection<PatientAlert> Alerts { get; set; } = new List<PatientAlert>();
    public ICollection<PatientAllergy> Allergies { get; set; } = new List<PatientAllergy>();
    public ICollection<PatientMedicalCondition> MedicalConditions { get; set; } = new List<PatientMedicalCondition>();
    public ICollection<PatientMedication> Medications { get; set; } = new List<PatientMedication>();
    public ICollection<MedicalHistoryReview> MedicalHistoryReviews { get; set; } = new List<MedicalHistoryReview>();
    public ICollection<VitalSignRecord> VitalSigns { get; set; } = new List<VitalSignRecord>();
    public ICollection<ToothConditionRecord> ToothConditions { get; set; } = new List<ToothConditionRecord>();
    public ICollection<PeriodontalChart> PeriodontalCharts { get; set; } = new List<PeriodontalChart>();
    public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    public ICollection<PatientDiagnosis> Diagnoses { get; set; } = new List<PatientDiagnosis>();
    public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
    public ICollection<Procedure> Procedures { get; set; } = new List<Procedure>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<RecallSchedule> Recalls { get; set; } = new List<RecallSchedule>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<PatientInsurance> InsurancePolicies { get; set; } = new List<PatientInsurance>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    public ICollection<RadiographRecord> Radiographs { get; set; } = new List<RadiographRecord>();
    public ICollection<PatientConsent> Consents { get; set; } = new List<PatientConsent>();
    public ICollection<CommunicationLog> Communications { get; set; } = new List<CommunicationLog>();
    public ICollection<LabCase> LabCases { get; set; } = new List<LabCase>();
    public ICollection<DentalImplant> Implants { get; set; } = new List<DentalImplant>();
    public ICollection<Referral> Referrals { get; set; } = new List<Referral>();

    // --- computed ----------------------------------------------------------
    public int? AgeYears
    {
        get
        {
            if (DateOfBirth is null) return null;
            var end = DateOfDeath ?? DateOnly.FromDateTime(DateTime.Today);
            var age = end.Year - DateOfBirth.Value.Year;
            if (end < DateOfBirth.Value.AddYears(age)) age--;
            return age < 0 ? null : age;
        }
    }

    public bool IsMinor => AgeYears is >= 0 and < 18;
    public string DisplayName => Name.Display;
    public string FullNameWithNumber => $"{Name.Display} ({PatientNumber})";
}

/// <summary>Someone connected to the patient: emergency contact, guardian, guarantor.</summary>
public class PatientContact : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public PersonName Name { get; set; } = new();
    public ContactDetails Contact { get; set; } = new();
    public Address? Address { get; set; }

    public ContactRelationship Relationship { get; set; } = ContactRelationship.Unknown;
    public ContactRole Role { get; set; } = ContactRole.Emergency;
    public bool IsPrimary { get; set; }
    public bool HasLegalAuthority { get; set; }
    public string? Notes { get; set; }
}

/// <summary>A banner shown prominently whenever the patient record is opened.</summary>
public class PatientAlert : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public AlertCategory Category { get; set; } = AlertCategory.Medical;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? RaisedBy { get; set; }
}

/// <summary>A file attached to the patient record.</summary>
public class PatientDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DocumentType DocumentType { get; set; } = DocumentType.Other;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? Tags { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public string? UploadedBy { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ProcedureId { get; set; }
    public bool IsClinicallySignificant { get; set; }
    public bool IsPatientVisible { get; set; }
}

/// <summary>Lifestyle and habit history that informs risk assessment.</summary>
public class SocialHistory : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly RecordedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? RecordedByStaffId { get; set; }

    public SmokingStatus SmokingStatus { get; set; } = SmokingStatus.Unknown;
    public int? CigarettesPerDay { get; set; }
    public int? YearsSmoked { get; set; }
    public DateOnly? QuitSmokingDate { get; set; }

    public AlcoholConsumption Alcohol { get; set; } = AlcoholConsumption.Unknown;
    public int? AlcoholUnitsPerWeek { get; set; }
    public bool RecreationalDrugUse { get; set; }
    public bool BetelNutOrTobaccoChewing { get; set; }

    public int? BrushingPerDay { get; set; }
    public bool UsesFluorideToothpaste { get; set; } = true;
    public bool Flosses { get; set; }
    public bool UsesInterdentalBrushes { get; set; }
    public bool UsesMouthwash { get; set; }
    public bool UsesElectricToothbrush { get; set; }
    public int? SugarIntakeEpisodesPerDay { get; set; }
    public bool AcidicDrinkConsumption { get; set; }
    public bool Bruxism { get; set; }
    public bool NailBiting { get; set; }
    public bool MouthBreathing { get; set; }
    public OralHygieneRating OralHygiene { get; set; } = OralHygieneRating.Fair;
    public bool DentalAnxiety { get; set; }
    public int? AnxietyScore { get; set; }
    public string? Notes { get; set; }
}
