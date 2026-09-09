using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>
/// Reference row for one tooth position. Seeded once with the 32 permanent and
/// 20 primary teeth, keyed by FDI two-digit notation.
/// </summary>
public class Tooth : BaseEntity, IGlobalEntity
{
    /// <summary>FDI / ISO 3950 notation, e.g. 11, 26, 48, 55.</summary>
    public int FdiNumber { get; set; }

    /// <summary>Universal numbering system: 1-32 permanent, A-T primary.</summary>
    public string UniversalNumber { get; set; } = string.Empty;

    /// <summary>Palmer notation, e.g. "UR1".</summary>
    public string PalmerNotation { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;

    public DentalArch Arch { get; set; }
    public Quadrant Quadrant { get; set; }
    public ToothType ToothType { get; set; }
    public bool IsPrimary { get; set; }

    /// <summary>Position 1-8 within the quadrant, counting from the midline.</summary>
    public int PositionInQuadrant { get; set; }

    public int RootCount { get; set; } = 1;
    public int CanalCount { get; set; } = 1;
    public bool HasOcclusalSurface { get; set; }

    /// <summary>Surfaces that are valid for this tooth.</summary>
    public ToothSurface ValidSurfaces { get; set; }

    /// <summary>Left-to-right ordering used to lay out the odontogram.</summary>
    public int ChartOrder { get; set; }

    public ICollection<ToothConditionRecord> Conditions { get; set; } = new List<ToothConditionRecord>();

    public bool IsAnterior => ToothType is ToothType.Incisor or ToothType.Canine;
    public bool IsPosterior => !IsAnterior;
    public override string ToString() => $"{FdiNumber} {Name}";
}

/// <summary>
/// One finding or restoration on a tooth. The odontogram is the union of these
/// records; historic entries are kept so the chart can be replayed over time.
/// </summary>
public class ToothConditionRecord : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid ToothId { get; set; }
    public Tooth? Tooth { get; set; }

    public ToothSurface Surfaces { get; set; } = ToothSurface.None;
    public ToothConditionType ConditionType { get; set; } = ToothConditionType.Caries;
    public ChartEntryStatus Status { get; set; } = ChartEntryStatus.Existing;
    public RestorationMaterial Material { get; set; } = RestorationMaterial.None;

    public DateOnly RecordedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? RecordedByStaffId { get; set; }
    public Staff? RecordedByStaff { get; set; }

    /// <summary>Set when this entry was created by, or completed through, a procedure.</summary>
    public Guid? ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }
    public Guid? TreatmentPlanItemId { get; set; }

    /// <summary>Chained history: the entry this one supersedes.</summary>
    public Guid? SupersedesId { get; set; }
    public ToothConditionRecord? Supersedes { get; set; }

    public MobilityGrade Mobility { get; set; } = MobilityGrade.None;
    public FurcationGrade Furcation { get; set; } = FurcationGrade.None;
    public int? SeverityScore { get; set; }
    public string? ShadeReference { get; set; }
    public string? ColourHex { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public string SurfaceCode => SurfaceNotation.ToCode(Surfaces);
}

/// <summary>Converts the surface flags to and from clinical shorthand such as "MOD".</summary>
public static class SurfaceNotation
{
    private static readonly (ToothSurface Flag, char Letter)[] Map =
    {
        (ToothSurface.Mesial,   'M'),
        (ToothSurface.Occlusal, 'O'),
        (ToothSurface.Incisal,  'I'),
        (ToothSurface.Distal,   'D'),
        (ToothSurface.Buccal,   'B'),
        (ToothSurface.Lingual,  'L'),
        (ToothSurface.Palatal,  'P'),
        (ToothSurface.Cervical, 'C'),
        (ToothSurface.Root,     'R'),
    };

    public static string ToCode(ToothSurface surfaces)
    {
        if (surfaces == ToothSurface.None) return string.Empty;
        if (surfaces.HasFlag(ToothSurface.Whole)) return "Whole";

        var chars = Map.Where(m => surfaces.HasFlag(m.Flag)).Select(m => m.Letter);
        return new string(chars.ToArray());
    }

    public static ToothSurface Parse(string? code)
    {
        var result = ToothSurface.None;
        if (string.IsNullOrWhiteSpace(code)) return result;
        if (code.Equals("whole", StringComparison.OrdinalIgnoreCase)) return ToothSurface.Whole;

        foreach (var ch in code.ToUpperInvariant())
        {
            var match = Map.FirstOrDefault(m => m.Letter == ch);
            if (match.Flag != default || ch == 'M') result |= match.Flag;
        }
        return result;
    }

    public static IReadOnlyList<ToothSurface> Split(ToothSurface surfaces) =>
        Map.Where(m => surfaces.HasFlag(m.Flag)).Select(m => m.Flag).ToList();
}

/// <summary>A full-mouth periodontal examination.</summary>
public class PeriodontalChart : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly ExamDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? ExaminerStaffId { get; set; }
    public Staff? ExaminerStaff { get; set; }
    public Guid? AppointmentId { get; set; }

    public PeriodontalDiagnosis Diagnosis { get; set; } = PeriodontalDiagnosis.Healthy;

    /// <summary>Percentage of sites bleeding on probing.</summary>
    public decimal? BleedingOnProbingPercent { get; set; }
    public decimal? PlaqueScorePercent { get; set; }
    public decimal? CalculusScorePercent { get; set; }

    /// <summary>Basic Periodontal Examination sextant codes 0-4 plus optional '*'.</summary>
    public string? BpeUpperRight { get; set; }
    public string? BpeUpperAnterior { get; set; }
    public string? BpeUpperLeft { get; set; }
    public string? BpeLowerRight { get; set; }
    public string? BpeLowerAnterior { get; set; }
    public string? BpeLowerLeft { get; set; }

    public bool IsFullMouth { get; set; } = true;
    public string? RiskFactors { get; set; }
    public string? TreatmentRecommendation { get; set; }
    public DateOnly? NextReviewDue { get; set; }
    public string? Notes { get; set; }

    public ICollection<PeriodontalMeasurement> Measurements { get; set; } = new List<PeriodontalMeasurement>();

    public int SitesRecorded => Measurements.Count;

    public int DeepPocketCount => Measurements.Count(m => m.PocketDepthMm >= 5);

    public decimal? MeanPocketDepth =>
        Measurements.Count == 0
            ? null
            : Math.Round(Measurements.Average(m => (decimal)m.PocketDepthMm), 2);

    public decimal? CalculatedBleedingPercent =>
        Measurements.Count == 0
            ? null
            : Math.Round(100m * Measurements.Count(m => m.BleedingOnProbing) / Measurements.Count, 1);
}

/// <summary>One probing site. Six per tooth in a standard full-mouth chart.</summary>
public class PeriodontalMeasurement : TenantEntity
{
    public Guid PeriodontalChartId { get; set; }
    public PeriodontalChart? PeriodontalChart { get; set; }

    public Guid ToothId { get; set; }
    public Tooth? Tooth { get; set; }

    public PeriodontalSite Site { get; set; }

    /// <summary>Probing pocket depth in millimetres.</summary>
    public int PocketDepthMm { get; set; }

    /// <summary>Gingival margin relative to the CEJ. Negative means recession.</summary>
    public int GingivalMarginMm { get; set; }

    public bool BleedingOnProbing { get; set; }
    public bool Suppuration { get; set; }
    public bool PlaquePresent { get; set; }
    public bool CalculusPresent { get; set; }

    public MobilityGrade Mobility { get; set; } = MobilityGrade.None;
    public FurcationGrade Furcation { get; set; } = FurcationGrade.None;
    public int? KeratinisedTissueMm { get; set; }
    public bool IsImplantSite { get; set; }
    public bool IsMissing { get; set; }

    /// <summary>Clinical attachment loss, derived from pocket depth and gingival margin.</summary>
    public int ClinicalAttachmentLevelMm => PocketDepthMm - GingivalMarginMm;

    public int RecessionMm => GingivalMarginMm < 0 ? -GingivalMarginMm : 0;
}
