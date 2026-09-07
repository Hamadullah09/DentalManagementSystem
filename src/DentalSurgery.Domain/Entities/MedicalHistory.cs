using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>Catalogue of systemic conditions relevant to dental care.</summary>
public class MedicalCondition : LookupEntity
{
    public string? Icd10Code { get; set; }
    public string Category { get; set; } = "General";

    /// <summary>Antibiotic prophylaxis indicated before invasive procedures.</summary>
    public bool RequiresAntibioticProphylaxis { get; set; }
    public bool IncreasesBleedingRisk { get; set; }
    public bool AffectsAnaesthesia { get; set; }
    public bool AffectsHealing { get; set; }
    public bool ContraindicatesAdrenaline { get; set; }
    public bool RequiresSteroidCover { get; set; }
    public AlertSeverity DefaultSeverity { get; set; } = AlertSeverity.Medium;
    public string? ClinicalGuidance { get; set; }

    public ICollection<PatientMedicalCondition> PatientLinks { get; set; } = new List<PatientMedicalCondition>();
}

/// <summary>A condition recorded against a specific patient.</summary>
public class PatientMedicalCondition : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? MedicalConditionId { get; set; }
    public MedicalCondition? MedicalCondition { get; set; }

    /// <summary>Used when the condition is not in the catalogue.</summary>
    public string? FreeTextCondition { get; set; }

    public ConditionStatus Status { get; set; } = ConditionStatus.Active;
    public DateOnly? DiagnosedOn { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;
    public string? ManagedBy { get; set; }
    public string? Notes { get; set; }

    public string DisplayName => MedicalCondition?.Name ?? FreeTextCondition ?? "Unspecified condition";
}

/// <summary>Catalogue of allergens.</summary>
public class Allergen : LookupEntity
{
    public AllergyType AllergyType { get; set; } = AllergyType.Drug;
    public string? CrossReactants { get; set; }
    public bool IsCommonInDentistry { get; set; }
}

/// <summary>An allergy recorded against a patient.</summary>
public class PatientAllergy : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? AllergenId { get; set; }
    public Allergen? Allergen { get; set; }
    public string? FreeTextAllergen { get; set; }

    public AllergyType AllergyType { get; set; } = AllergyType.Drug;
    public AllergySeverity Severity { get; set; } = AllergySeverity.Moderate;
    public string? Reaction { get; set; }
    public DateOnly? OnsetDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? VerifiedBy { get; set; }
    public string? Notes { get; set; }

    public string DisplayName => Allergen?.Name ?? FreeTextAllergen ?? "Unspecified allergen";
    public bool IsCritical => Severity >= AllergySeverity.Severe;
}

/// <summary>Drug catalogue used for prescribing and for recording current medication.</summary>
public class Medication : LookupEntity
{
    public string? GenericName { get; set; }
    public string? BrandNames { get; set; }
    public string? DrugClass { get; set; }
    public MedicationForm Form { get; set; } = MedicationForm.Tablet;
    public string? Strength { get; set; }
    public MedicationRoute DefaultRoute { get; set; } = MedicationRoute.Oral;

    public string? DefaultDosage { get; set; }
    public string? DefaultFrequency { get; set; }
    public int? DefaultDurationDays { get; set; }
    public int? DefaultQuantity { get; set; }
    public string? DefaultInstructions { get; set; }

    public bool IsControlledDrug { get; set; }
    public string? ControlledSchedule { get; set; }
    public bool IsAntibiotic { get; set; }
    public bool IsAnalgesic { get; set; }
    public bool IsAnaesthetic { get; set; }
    public bool ContraindicatedInPregnancy { get; set; }
    public bool ContraindicatedInBreastfeeding { get; set; }
    public bool RequiresRenalAdjustment { get; set; }
    public string? Interactions { get; set; }
    public string? Contraindications { get; set; }
    public string? SideEffects { get; set; }
    public decimal? UnitCost { get; set; }
}

/// <summary>A drug the patient is currently or was previously taking.</summary>
public class PatientMedication : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public string? FreeTextMedication { get; set; }

    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public MedicationRoute Route { get; set; } = MedicationRoute.Oral;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string? PrescribedBy { get; set; }
    public string? Indication { get; set; }
    public string? Notes { get; set; }

    public string DisplayName => Medication?.Name ?? FreeTextMedication ?? "Unspecified medication";
}

/// <summary>A point-in-time confirmation that the medical history was reviewed with the patient.</summary>
public class MedicalHistoryReview : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly ReviewDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? ReviewedByStaffId { get; set; }
    public Staff? ReviewedByStaff { get; set; }
    public Guid? AppointmentId { get; set; }

    public bool NoChangesReported { get; set; }
    public string? ChangesSummary { get; set; }

    public bool IsPregnant { get; set; }
    public int? WeeksPregnant { get; set; }
    public bool IsBreastfeeding { get; set; }
    public bool TakingAnticoagulants { get; set; }
    public bool TakingBisphosphonates { get; set; }
    public bool TakingImmunosuppressants { get; set; }
    public bool HasPacemaker { get; set; }
    public bool HasProstheticJoint { get; set; }
    public bool HasProstheticHeartValve { get; set; }
    public bool HistoryOfEndocarditis { get; set; }
    public bool HistoryOfRadiotherapyToHeadOrNeck { get; set; }
    public bool HistoryOfChemotherapy { get; set; }
    public bool RequiresAntibioticProphylaxis { get; set; }

    public AsaClassification AsaClassification { get; set; } = AsaClassification.AsaI;
    public bool PatientSignatureObtained { get; set; }
    public string? SignatureData { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Observations taken before treatment, especially before sedation or surgery.</summary>
public class VitalSignRecord : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? RecordedByStaffId { get; set; }
    public Staff? RecordedByStaff { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ProcedureId { get; set; }

    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? PulseBpm { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? BloodGlucoseMmolL { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public string? Notes { get; set; }

    public decimal? Bmi =>
        WeightKg is > 0 && HeightCm is > 0
            ? Math.Round(WeightKg.Value / (HeightCm.Value / 100m * (HeightCm.Value / 100m)), 1)
            : null;

    public string? BloodPressure =>
        SystolicBp.HasValue && DiastolicBp.HasValue ? $"{SystolicBp}/{DiastolicBp}" : null;

    /// <summary>Flags readings that warrant deferring elective treatment.</summary>
    public bool IsHypertensiveCrisis => SystolicBp >= 180 || DiastolicBp >= 120;
}
