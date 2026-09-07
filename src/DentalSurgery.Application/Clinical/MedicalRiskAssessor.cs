using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Application.Clinical;

public enum RiskLevel { None = 0, Low = 1, Moderate = 2, High = 3, Critical = 4 }

/// <summary>A single actionable warning shown to the clinician before treatment.</summary>
public record RiskFlag(string Code, string Title, string Detail, RiskLevel Level, string Category)
{
    public string BadgeClass => Level switch
    {
        RiskLevel.Critical => "bg-danger",
        RiskLevel.High => "bg-danger-subtle text-danger-emphasis",
        RiskLevel.Moderate => "bg-warning-subtle text-warning-emphasis",
        RiskLevel.Low => "bg-info-subtle text-info-emphasis",
        _ => "bg-secondary-subtle text-secondary-emphasis"
    };
}

/// <summary>Consolidated pre-treatment risk picture for one patient.</summary>
public class MedicalRiskProfile
{
    public IReadOnlyList<RiskFlag> Flags { get; init; } = Array.Empty<RiskFlag>();
    public RiskLevel OverallLevel => Flags.Count == 0 ? RiskLevel.None : Flags.Max(f => f.Level);

    public bool RequiresAntibioticProphylaxis { get; init; }
    public bool BleedingRisk { get; init; }
    public bool AdrenalineCaution { get; init; }
    public bool SedationCaution { get; init; }
    public bool DelayedHealingRisk { get; init; }
    public bool MronjRisk { get; init; }
    public bool LatexAllergy { get; init; }
    public AsaClassification EstimatedAsa { get; init; } = AsaClassification.AsaI;

    public IEnumerable<RiskFlag> Critical => Flags.Where(f => f.Level >= RiskLevel.High);
    public bool HasAnyFlags => Flags.Count > 0;
}

/// <summary>
/// Derives pre-treatment warnings from the recorded medical history. This is
/// decision support, not a substitute for clinical judgement.
/// </summary>
public class MedicalRiskAssessor
{
    private static readonly string[] AnticoagulantKeywords =
    {
        "warfarin", "apixaban", "rivaroxaban", "dabigatran", "edoxaban",
        "clopidogrel", "heparin", "aspirin", "ticagrelor", "prasugrel"
    };

    private static readonly string[] BisphosphonateKeywords =
    {
        "alendron", "risedron", "zoledron", "pamidron", "ibandron",
        "denosumab", "prolia", "xgeva", "romosozumab"
    };

    private static readonly string[] ImmunosuppressantKeywords =
    {
        "methotrexate", "ciclosporin", "cyclosporine", "tacrolimus", "azathioprine",
        "prednisolone", "prednisone", "adalimumab", "infliximab", "rituximab", "mycophenolate"
    };

    public MedicalRiskProfile Assess(
        Patient patient,
        IEnumerable<PatientAllergy>? allergies = null,
        IEnumerable<PatientMedicalCondition>? conditions = null,
        IEnumerable<PatientMedication>? medications = null,
        MedicalHistoryReview? latestReview = null)
    {
        var flags = new List<RiskFlag>();

        var allergyList = (allergies ?? patient.Allergies).Where(a => a.IsActive).ToList();
        var conditionList = (conditions ?? patient.MedicalConditions)
            .Where(c => c.Status is ConditionStatus.Active or ConditionStatus.Chronic).ToList();
        var medicationList = (medications ?? patient.Medications).Where(m => m.IsCurrent).ToList();

        // ---- allergies ----------------------------------------------------
        var latexAllergy = false;
        foreach (var allergy in allergyList)
        {
            var level = allergy.Severity switch
            {
                AllergySeverity.Anaphylaxis => RiskLevel.Critical,
                AllergySeverity.Severe => RiskLevel.High,
                AllergySeverity.Moderate => RiskLevel.Moderate,
                _ => RiskLevel.Low
            };

            flags.Add(new RiskFlag(
                $"ALLERGY:{allergy.DisplayName}",
                $"Allergy: {allergy.DisplayName}",
                string.IsNullOrWhiteSpace(allergy.Reaction)
                    ? $"{allergy.Severity} reaction recorded."
                    : $"{allergy.Severity}: {allergy.Reaction}",
                level,
                "Allergy"));

            if (allergy.AllergyType == AllergyType.Latex ||
                allergy.DisplayName.Contains("latex", StringComparison.OrdinalIgnoreCase))
            {
                latexAllergy = true;
            }
        }

        if (latexAllergy)
        {
            flags.Add(new RiskFlag("LATEX", "Latex-free protocol required",
                "Use latex-free gloves, dam and equipment for this patient.",
                RiskLevel.High, "Allergy"));
        }

        // ---- conditions ---------------------------------------------------
        var prophylaxis = latestReview?.RequiresAntibioticProphylaxis ?? false;
        var bleeding = false;
        var adrenaline = false;
        var healing = false;
        var sedation = false;

        foreach (var pc in conditionList)
        {
            var cat = pc.MedicalCondition;
            var level = pc.Severity switch
            {
                AlertSeverity.Critical => RiskLevel.Critical,
                AlertSeverity.High => RiskLevel.High,
                AlertSeverity.Medium => RiskLevel.Moderate,
                _ => RiskLevel.Low
            };

            var detail = cat?.ClinicalGuidance ?? pc.Notes ?? "Recorded in medical history.";
            flags.Add(new RiskFlag($"COND:{pc.DisplayName}", pc.DisplayName, detail, level, "Medical"));

            if (cat is null) continue;
            prophylaxis |= cat.RequiresAntibioticProphylaxis;
            bleeding |= cat.IncreasesBleedingRisk;
            adrenaline |= cat.ContraindicatesAdrenaline;
            healing |= cat.AffectsHealing;
            sedation |= cat.AffectsAnaesthesia;
        }

        // ---- medications --------------------------------------------------
        var mronj = latestReview?.TakingBisphosphonates ?? false;
        foreach (var med in medicationList)
        {
            var name = med.DisplayName.ToLowerInvariant();
            var generic = med.Medication?.GenericName?.ToLowerInvariant() ?? string.Empty;
            var haystack = name + " " + generic;

            if (AnticoagulantKeywords.Any(k => haystack.Contains(k)))
            {
                bleeding = true;
                flags.Add(new RiskFlag($"MED:ANTICOAG:{med.DisplayName}",
                    $"Anticoagulant / antiplatelet: {med.DisplayName}",
                    "Check INR where applicable and plan local haemostatic measures before extraction or surgery.",
                    RiskLevel.High, "Medication"));
            }

            if (BisphosphonateKeywords.Any(k => haystack.Contains(k)))
            {
                mronj = true;
                flags.Add(new RiskFlag($"MED:BISPHOS:{med.DisplayName}",
                    $"Antiresorptive therapy: {med.DisplayName}",
                    "Risk of medication-related osteonecrosis of the jaw. Consider conservative alternatives to extraction.",
                    RiskLevel.High, "Medication"));
            }

            if (ImmunosuppressantKeywords.Any(k => haystack.Contains(k)))
            {
                healing = true;
                flags.Add(new RiskFlag($"MED:IMMUNO:{med.DisplayName}",
                    $"Immunosuppressant: {med.DisplayName}",
                    "Increased infection risk and delayed healing. Consider steroid cover if on long-term corticosteroids.",
                    RiskLevel.Moderate, "Medication"));
            }
        }

        // ---- history review -----------------------------------------------
        if (latestReview is not null)
        {
            if (latestReview.IsPregnant)
            {
                flags.Add(new RiskFlag("PREGNANCY", "Pregnant",
                    latestReview.WeeksPregnant.HasValue
                        ? $"Approximately {latestReview.WeeksPregnant} weeks. Avoid elective radiographs; second trimester preferred for treatment."
                        : "Avoid elective radiographs; second trimester preferred for treatment.",
                    RiskLevel.High, "Medical"));
            }

            if (latestReview.IsBreastfeeding)
            {
                flags.Add(new RiskFlag("BREASTFEEDING", "Breastfeeding",
                    "Check prescribing safety before issuing any medication.", RiskLevel.Moderate, "Medical"));
            }

            if (latestReview.HasPacemaker)
            {
                flags.Add(new RiskFlag("PACEMAKER", "Cardiac device fitted",
                    "Avoid electrosurgery and ultrasonic scalers near the device.", RiskLevel.High, "Medical"));
            }

            if (latestReview.HistoryOfEndocarditis || latestReview.HasProstheticHeartValve)
            {
                prophylaxis = true;
                flags.Add(new RiskFlag("ENDOCARDITIS", "Infective endocarditis risk",
                    "Antibiotic prophylaxis indicated for invasive dental procedures. Confirm against current guidance.",
                    RiskLevel.Critical, "Medical"));
            }

            if (latestReview.HistoryOfRadiotherapyToHeadOrNeck)
            {
                healing = true;
                flags.Add(new RiskFlag("ORN", "Head and neck radiotherapy",
                    "Risk of osteoradionecrosis. Avoid extractions where possible and consider specialist referral.",
                    RiskLevel.High, "Medical"));
            }

            if (latestReview.TakingAnticoagulants) bleeding = true;
            if (latestReview.AsaClassification >= AsaClassification.AsaIII) sedation = true;
        }

        // ---- lifestyle ----------------------------------------------------
        if (patient.AgeYears is >= 75)
        {
            flags.Add(new RiskFlag("AGE", "Elderly patient",
                "Consider polypharmacy, frailty and shorter appointment tolerance.", RiskLevel.Low, "General"));
        }

        // ---- derived summary flags -----------------------------------------
        if (prophylaxis)
        {
            flags.Add(new RiskFlag("PROPHYLAXIS", "Antibiotic prophylaxis required",
                "Confirm regimen and administer before invasive treatment.", RiskLevel.High, "Protocol"));
        }

        if (bleeding)
        {
            flags.Add(new RiskFlag("BLEEDING", "Increased bleeding risk",
                "Plan local haemostatic measures. Avoid multiple extractions in a single visit.",
                RiskLevel.High, "Protocol"));
        }

        if (adrenaline)
        {
            flags.Add(new RiskFlag("ADRENALINE", "Limit adrenaline-containing anaesthetic",
                "Use a plain anaesthetic or restrict the total adrenaline dose.", RiskLevel.Moderate, "Protocol"));
        }

        var asa = EstimateAsa(conditionList, latestReview);

        return new MedicalRiskProfile
        {
            Flags = flags
                .GroupBy(f => f.Code)
                .Select(g => g.OrderByDescending(f => f.Level).First())
                .OrderByDescending(f => f.Level)
                .ThenBy(f => f.Title)
                .ToList(),
            RequiresAntibioticProphylaxis = prophylaxis,
            BleedingRisk = bleeding,
            AdrenalineCaution = adrenaline,
            SedationCaution = sedation,
            DelayedHealingRisk = healing,
            MronjRisk = mronj,
            LatexAllergy = latexAllergy,
            EstimatedAsa = asa
        };
    }

    private static AsaClassification EstimateAsa(
        IReadOnlyCollection<PatientMedicalCondition> conditions,
        MedicalHistoryReview? review)
    {
        if (review is not null) return review.AsaClassification;
        if (conditions.Count == 0) return AsaClassification.AsaI;

        var severe = conditions.Count(c => c.Severity >= AlertSeverity.High);
        return severe switch
        {
            0 => AsaClassification.AsaII,
            1 => AsaClassification.AsaIII,
            _ => AsaClassification.AsaIV
        };
    }
}
