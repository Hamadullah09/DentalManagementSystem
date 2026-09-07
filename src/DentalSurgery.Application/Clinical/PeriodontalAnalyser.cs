using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Application.Clinical;

/// <summary>Summary statistics derived from a full-mouth periodontal chart.</summary>
public class PeriodontalSummary
{
    public int SitesRecorded { get; init; }
    public int TeethCharted { get; init; }
    public decimal MeanPocketDepth { get; init; }
    public int MaxPocketDepth { get; init; }
    public decimal MeanAttachmentLoss { get; init; }
    public int MaxAttachmentLoss { get; init; }

    public int SitesUnder4mm { get; init; }
    public int Sites4To5mm { get; init; }
    public int Sites6To7mm { get; init; }
    public int Sites8mmPlus { get; init; }

    public decimal BleedingPercent { get; init; }
    public decimal PlaquePercent { get; init; }
    public decimal SuppurationPercent { get; init; }
    public decimal RecessionPresentPercent { get; init; }

    public int MobileTeeth { get; init; }
    public int FurcationInvolvedTeeth { get; init; }

    public PeriodontalDiagnosis SuggestedDiagnosis { get; init; }
    public string StagingRationale { get; init; } = string.Empty;
    public int RecommendedRecallMonths { get; init; } = 6;
    public RiskLevel RiskLevel { get; init; }

    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Computes periodontal indices and suggests a stage/grade using the 2017
/// world workshop framework. Output is advisory; the clinician confirms.
/// </summary>
public class PeriodontalAnalyser
{
    public PeriodontalSummary Analyse(PeriodontalChart chart, int? patientAge = null, bool isSmoker = false, bool isDiabetic = false)
    {
        var sites = chart.Measurements.Where(m => !m.IsMissing).ToList();
        if (sites.Count == 0)
        {
            return new PeriodontalSummary
            {
                SuggestedDiagnosis = PeriodontalDiagnosis.Healthy,
                StagingRationale = "No sites recorded.",
                Recommendations = new[] { "Complete a full-mouth periodontal chart." }
            };
        }

        var maxPd = sites.Max(m => m.PocketDepthMm);
        var meanPd = Math.Round(sites.Average(m => (decimal)m.PocketDepthMm), 2);
        var calValues = sites.Select(m => m.ClinicalAttachmentLevelMm).ToList();
        var maxCal = calValues.Max();
        var meanCal = Math.Round(calValues.Average(v => (decimal)v), 2);

        var bleeding = Pct(sites.Count(m => m.BleedingOnProbing), sites.Count);
        var plaque = Pct(sites.Count(m => m.PlaquePresent), sites.Count);
        var suppuration = Pct(sites.Count(m => m.Suppuration), sites.Count);
        var recession = Pct(sites.Count(m => m.RecessionMm > 0), sites.Count);

        var teeth = sites.Select(m => m.ToothId).Distinct().ToList();
        var mobileTeeth = sites.Where(m => m.Mobility != MobilityGrade.None)
            .Select(m => m.ToothId).Distinct().Count();
        var furcationTeeth = sites.Where(m => m.Furcation != FurcationGrade.None)
            .Select(m => m.ToothId).Distinct().Count();

        var (diagnosis, rationale) = Stage(maxCal, maxPd, bleeding, mobileTeeth, furcationTeeth,
            teeth.Count, patientAge, isSmoker, isDiabetic);

        var risk = DetermineRisk(diagnosis, bleeding, maxPd, isSmoker, isDiabetic);
        var recall = risk switch
        {
            RiskLevel.Critical => 3,
            RiskLevel.High => 3,
            RiskLevel.Moderate => 4,
            RiskLevel.Low => 6,
            _ => 6
        };

        return new PeriodontalSummary
        {
            SitesRecorded = sites.Count,
            TeethCharted = teeth.Count,
            MeanPocketDepth = meanPd,
            MaxPocketDepth = maxPd,
            MeanAttachmentLoss = meanCal,
            MaxAttachmentLoss = maxCal,
            SitesUnder4mm = sites.Count(m => m.PocketDepthMm <= 3),
            Sites4To5mm = sites.Count(m => m.PocketDepthMm is >= 4 and <= 5),
            Sites6To7mm = sites.Count(m => m.PocketDepthMm is >= 6 and <= 7),
            Sites8mmPlus = sites.Count(m => m.PocketDepthMm >= 8),
            BleedingPercent = bleeding,
            PlaquePercent = plaque,
            SuppurationPercent = suppuration,
            RecessionPresentPercent = recession,
            MobileTeeth = mobileTeeth,
            FurcationInvolvedTeeth = furcationTeeth,
            SuggestedDiagnosis = diagnosis,
            StagingRationale = rationale,
            RecommendedRecallMonths = recall,
            RiskLevel = risk,
            Recommendations = BuildRecommendations(diagnosis, bleeding, plaque, maxPd, isSmoker, furcationTeeth)
        };
    }

    /// <summary>
    /// Derives a BPE sextant code (0-4) from the deepest pocket in the sextant,
    /// with '*' appended when furcation involvement or recession is present.
    /// </summary>
    public static string BpeCode(IEnumerable<PeriodontalMeasurement> sextantSites)
    {
        var list = sextantSites.Where(m => !m.IsMissing).ToList();
        if (list.Count == 0) return "X";

        var maxPd = list.Max(m => m.PocketDepthMm);
        var bleeding = list.Any(m => m.BleedingOnProbing);
        var calculus = list.Any(m => m.CalculusPresent);

        var code = maxPd switch
        {
            >= 6 => "4",
            >= 4 => "3",
            _ when calculus => "2",
            _ when bleeding => "1",
            _ => "0"
        };

        var furcationOrRecession =
            list.Any(m => m.Furcation != FurcationGrade.None) ||
            list.Any(m => m.ClinicalAttachmentLevelMm >= 7);

        return furcationOrRecession ? code + "*" : code;
    }

    private static (PeriodontalDiagnosis, string) Stage(
        int maxCal, int maxPd, decimal bleeding, int mobileTeeth, int furcationTeeth,
        int teethCharted, int? age, bool smoker, bool diabetic)
    {
        // Case definition first. A 1-3 mm sulcus with the gingival margin at the
        // CEJ is a healthy periodontium, not attachment loss, so periodontitis is
        // only considered once pocketing or true attachment loss is present.
        var meetsPeriodontitisDefinition = maxPd >= 4 || maxCal >= 4;

        if (!meetsPeriodontitisDefinition)
        {
            return bleeding >= 10m
                ? (PeriodontalDiagnosis.Gingivitis,
                    $"Pocketing within normal limits (deepest {maxPd} mm) with bleeding on probing at {bleeding}% of sites.")
                : (PeriodontalDiagnosis.Healthy,
                    $"Pocketing within normal limits (deepest {maxPd} mm) and bleeding at only {bleeding}% of sites.");
        }

        // Stage from interdental clinical attachment loss, escalated by complexity.
        var stage = maxCal switch
        {
            <= 2 => 1,
            <= 4 => 2,
            _ => 3
        };

        if (maxCal >= 5 && (mobileTeeth >= 3 || furcationTeeth >= 2 || maxPd >= 8)) stage = 4;
        if (stage == 3 && maxPd >= 6 && furcationTeeth >= 1) stage = 3;

        // Grade from risk modifiers, since historic radiographic bone loss is not modelled here.
        var grade = 'B';
        if (smoker || diabetic) grade = 'C';
        else if (age is >= 60 && maxCal <= 4) grade = 'A';

        var diagnosis = (stage, grade) switch
        {
            (1, 'A') => PeriodontalDiagnosis.PeriodontitisStageIGradeA,
            (1, 'C') => PeriodontalDiagnosis.PeriodontitisStageIGradeC,
            (1, _) => PeriodontalDiagnosis.PeriodontitisStageIGradeB,
            (2, 'A') => PeriodontalDiagnosis.PeriodontitisStageIIGradeA,
            (2, 'C') => PeriodontalDiagnosis.PeriodontitisStageIIGradeC,
            (2, _) => PeriodontalDiagnosis.PeriodontitisStageIIGradeB,
            (3, 'A') => PeriodontalDiagnosis.PeriodontitisStageIIIGradeA,
            (3, 'C') => PeriodontalDiagnosis.PeriodontitisStageIIIGradeC,
            (3, _) => PeriodontalDiagnosis.PeriodontitisStageIIIGradeB,
            (4, 'A') => PeriodontalDiagnosis.PeriodontitisStageIVGradeA,
            (4, 'C') => PeriodontalDiagnosis.PeriodontitisStageIVGradeC,
            _ => PeriodontalDiagnosis.PeriodontitisStageIVGradeB
        };

        var modifiers = new List<string>();
        if (smoker) modifiers.Add("smoker");
        if (diabetic) modifiers.Add("diabetic");
        var modifierText = modifiers.Count > 0 ? $" Grade raised by risk factors: {string.Join(", ", modifiers)}." : string.Empty;

        var rationale =
            $"Maximum interdental CAL {maxCal} mm and deepest pocket {maxPd} mm across {teethCharted} teeth " +
            $"give stage {stage}. Mobility on {mobileTeeth} teeth, furcation involvement on {furcationTeeth}.{modifierText}";

        return (diagnosis, rationale);
    }

    private static RiskLevel DetermineRisk(
        PeriodontalDiagnosis diagnosis, decimal bleeding, int maxPd, bool smoker, bool diabetic)
    {
        var score = 0;
        if (diagnosis >= PeriodontalDiagnosis.PeriodontitisStageIIIGradeA) score += 3;
        else if (diagnosis >= PeriodontalDiagnosis.PeriodontitisStageIGradeA) score += 2;
        else if (diagnosis == PeriodontalDiagnosis.Gingivitis) score += 1;

        if (bleeding >= 30m) score += 2;
        else if (bleeding >= 10m) score += 1;

        if (maxPd >= 6) score += 2;
        else if (maxPd >= 4) score += 1;

        if (smoker) score += 2;
        if (diabetic) score += 1;

        return score switch
        {
            >= 8 => RiskLevel.Critical,
            >= 6 => RiskLevel.High,
            >= 3 => RiskLevel.Moderate,
            >= 1 => RiskLevel.Low,
            _ => RiskLevel.None
        };
    }

    private static List<string> BuildRecommendations(
        PeriodontalDiagnosis diagnosis, decimal bleeding, decimal plaque, int maxPd, bool smoker, int furcationTeeth)
    {
        var list = new List<string>();

        if (plaque >= 30m) list.Add("Reinforce oral hygiene instruction; plaque control is inadequate.");
        if (bleeding >= 30m) list.Add("Widespread inflammation - schedule non-surgical therapy and review in 8-12 weeks.");
        if (maxPd >= 6) list.Add("Sites of 6 mm or more present - consider subgingival instrumentation under local anaesthetic.");
        if (maxPd >= 8) list.Add("Consider referral for specialist periodontal assessment.");
        if (furcationTeeth > 0) list.Add($"Furcation involvement on {furcationTeeth} tooth/teeth - assess prognosis individually.");
        if (smoker) list.Add("Offer smoking cessation support; smoking materially worsens the prognosis.");
        if (diagnosis == PeriodontalDiagnosis.Healthy && list.Count == 0) list.Add("Maintain current regimen and routine recall.");
        if (diagnosis == PeriodontalDiagnosis.Gingivitis) list.Add("Professional cleaning plus oral hygiene instruction; re-evaluate at 3 months.");

        return list;
    }

    private static decimal Pct(int numerator, int denominator) =>
        denominator == 0 ? 0m : Math.Round(100m * numerator / denominator, 1);
}
