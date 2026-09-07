using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Application.Clinical;

/// <summary>The current state of one tooth, collapsed from its condition history.</summary>
public class ToothState
{
    public Tooth Tooth { get; init; } = null!;
    public IReadOnlyList<ToothConditionRecord> Existing { get; init; } = Array.Empty<ToothConditionRecord>();
    public IReadOnlyList<ToothConditionRecord> Planned { get; init; } = Array.Empty<ToothConditionRecord>();
    public IReadOnlyList<ToothConditionRecord> Completed { get; init; } = Array.Empty<ToothConditionRecord>();
    public IReadOnlyList<ToothConditionRecord> Watch { get; init; } = Array.Empty<ToothConditionRecord>();

    public bool IsMissing { get; init; }
    public bool IsImplant { get; init; }
    public bool IsCrowned { get; init; }
    public bool IsRootTreated { get; init; }
    public bool HasCaries { get; init; }
    public bool HasRestoration { get; init; }
    public bool HasPlannedWork { get; init; }
    public MobilityGrade Mobility { get; init; }

    /// <summary>Surfaces carrying an active restoration, keyed for odontogram fill.</summary>
    public ToothSurface RestoredSurfaces { get; init; }
    public ToothSurface CariousSurfaces { get; init; }
    public ToothSurface PlannedSurfaces { get; init; }

    public IEnumerable<ToothConditionRecord> AllActive =>
        Existing.Concat(Completed).Concat(Planned).Concat(Watch);

    public string StatusSummary
    {
        get
        {
            if (IsMissing) return "Missing";
            var parts = new List<string>();
            if (IsImplant) parts.Add("Implant");
            if (IsCrowned) parts.Add("Crown");
            if (IsRootTreated) parts.Add("RCT");
            if (HasRestoration) parts.Add("Restored");
            if (HasCaries) parts.Add("Caries");
            if (HasPlannedWork) parts.Add("Planned");
            return parts.Count == 0 ? "Sound" : string.Join(", ", parts);
        }
    }

    /// <summary>Colour used to fill the tooth outline in the odontogram.</summary>
    public string FillColour =>
        IsMissing ? "#e9ecef"
        : HasCaries ? "#dc3545"
        : HasPlannedWork ? "#0d6efd"
        : IsImplant ? "#6f42c1"
        : IsCrowned ? "#ffc107"
        : HasRestoration ? "#20c997"
        : "#ffffff";
}

/// <summary>The whole mouth, arranged for display.</summary>
public class DentalChart
{
    public Guid PatientId { get; init; }
    public Dentition Dentition { get; init; } = Dentition.Permanent;
    public IReadOnlyList<ToothState> UpperRow { get; init; } = Array.Empty<ToothState>();
    public IReadOnlyList<ToothState> LowerRow { get; init; } = Array.Empty<ToothState>();

    public IEnumerable<ToothState> AllTeeth => UpperRow.Concat(LowerRow);

    public int MissingCount => AllTeeth.Count(t => t.IsMissing);
    public int PresentCount => AllTeeth.Count(t => !t.IsMissing);
    public int CariousCount => AllTeeth.Count(t => t.HasCaries);
    public int RestoredCount => AllTeeth.Count(t => t.HasRestoration);
    public int CrownedCount => AllTeeth.Count(t => t.IsCrowned);
    public int ImplantCount => AllTeeth.Count(t => t.IsImplant);
    public int RootTreatedCount => AllTeeth.Count(t => t.IsRootTreated);
    public int PlannedWorkCount => AllTeeth.Count(t => t.HasPlannedWork);

    /// <summary>
    /// DMFT index: decayed, missing and filled permanent teeth. A standard
    /// epidemiological measure of caries experience.
    /// </summary>
    public int Dmft => AllTeeth.Count(t =>
        !t.Tooth.IsPrimary && (t.HasCaries || t.IsMissing || t.HasRestoration || t.IsCrowned));

    public ToothState? ByFdi(int fdi) => AllTeeth.FirstOrDefault(t => t.Tooth.FdiNumber == fdi);
}

/// <summary>Collapses the append-only condition history into a displayable chart.</summary>
public class DentalChartBuilder
{
    public DentalChart Build(
        Guid patientId,
        IEnumerable<Tooth> teeth,
        IEnumerable<ToothConditionRecord> records,
        Dentition dentition = Dentition.Permanent,
        DateOnly? asOf = null)
    {
        var cutoff = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        var relevantTeeth = teeth
            .Where(t => dentition switch
            {
                Dentition.Permanent => !t.IsPrimary,
                Dentition.Primary => t.IsPrimary,
                _ => true
            })
            .OrderBy(t => t.ChartOrder)
            .ToList();

        var byTooth = records
            .Where(r => r.IsActive && !r.IsDeleted && r.RecordedOn <= cutoff)
            .GroupBy(r => r.ToothId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var states = relevantTeeth
            .Select(tooth => BuildState(tooth, byTooth.TryGetValue(tooth.Id, out var recs) ? recs : new List<ToothConditionRecord>()))
            .ToList();

        return new DentalChart
        {
            PatientId = patientId,
            Dentition = dentition,
            UpperRow = states.Where(s => s.Tooth.Arch == DentalArch.Upper).OrderBy(s => s.Tooth.ChartOrder).ToList(),
            LowerRow = states.Where(s => s.Tooth.Arch == DentalArch.Lower).OrderBy(s => s.Tooth.ChartOrder).ToList()
        };
    }

    private static ToothState BuildState(Tooth tooth, List<ToothConditionRecord> records)
    {
        var existing = records.Where(r => r.Status == ChartEntryStatus.Existing).ToList();
        var planned = records.Where(r => r.Status is ChartEntryStatus.Planned or ChartEntryStatus.InProgress).ToList();
        var completed = records.Where(r => r.Status == ChartEntryStatus.Completed).ToList();
        var watch = records.Where(r => r.Status == ChartEntryStatus.Watch).ToList();

        var settled = existing.Concat(completed).ToList();

        var missing = settled.Any(r => r.ConditionType is
            ToothConditionType.Missing or ToothConditionType.Extracted or ToothConditionType.Unerupted);

        var implant = settled.Any(r => r.ConditionType is
            ToothConditionType.Implant or ToothConditionType.ImplantCrown);

        var crowned = settled.Any(r => r.ConditionType is
            ToothConditionType.Crown or ToothConditionType.BridgeAbutment or ToothConditionType.ImplantCrown);

        var rootTreated = settled.Any(r => r.ConditionType is
            ToothConditionType.RootCanalTreated or ToothConditionType.PostAndCore);

        var restorationTypes = new[]
        {
            ToothConditionType.Restoration, ToothConditionType.Inlay, ToothConditionType.Onlay,
            ToothConditionType.Veneer, ToothConditionType.TemporaryRestoration, ToothConditionType.Sealant
        };

        var caries = settled.Where(r => r.ConditionType == ToothConditionType.Caries).ToList();
        var restorations = settled.Where(r => restorationTypes.Contains(r.ConditionType)).ToList();

        return new ToothState
        {
            Tooth = tooth,
            Existing = existing,
            Planned = planned,
            Completed = completed,
            Watch = watch,
            IsMissing = missing,
            IsImplant = implant,
            IsCrowned = crowned,
            IsRootTreated = rootTreated,
            HasCaries = caries.Count > 0,
            HasRestoration = restorations.Count > 0,
            HasPlannedWork = planned.Count > 0,
            Mobility = settled.Count == 0 ? MobilityGrade.None : settled.Max(r => r.Mobility),
            RestoredSurfaces = Combine(restorations),
            CariousSurfaces = Combine(caries),
            PlannedSurfaces = Combine(planned)
        };
    }

    private static ToothSurface Combine(IEnumerable<ToothConditionRecord> records)
    {
        var result = ToothSurface.None;
        foreach (var record in records) result |= record.Surfaces;
        return result;
    }
}
