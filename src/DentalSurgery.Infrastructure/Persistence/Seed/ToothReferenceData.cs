using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Infrastructure.Persistence.Seed;

/// <summary>
/// Generates the 32 permanent and 20 primary tooth positions using FDI notation,
/// with Universal and Palmer cross-references. Identifiers are derived from the
/// FDI number so the seed is idempotent across runs.
/// </summary>
public static class ToothReferenceData
{
    /// <summary>Stable identifier for a tooth position, derived from its FDI number.</summary>
    public static Guid IdFor(int fdiNumber) => new($"00000000-0000-0000-0000-{fdiNumber:D12}");

    private static readonly string[] PermanentNames =
    {
        "", "Central Incisor", "Lateral Incisor", "Canine", "First Premolar",
        "Second Premolar", "First Molar", "Second Molar", "Third Molar"
    };

    private static readonly string[] PrimaryNames =
    {
        "", "Central Incisor", "Lateral Incisor", "Canine", "First Molar", "Second Molar"
    };

    public static List<Tooth> Build()
    {
        var teeth = new List<Tooth>();

        // ---- permanent dentition, quadrants 1-4 ---------------------------
        // Chart order runs left to right as the clinician sees the patient.
        var upperOrder = 1;
        foreach (var position in Enumerable.Range(1, 8).Reverse())   // UR8 .. UR1
            teeth.Add(Permanent(10 + position, Quadrant.UpperRight, position, upperOrder++));
        foreach (var position in Enumerable.Range(1, 8))             // UL1 .. UL8
            teeth.Add(Permanent(20 + position, Quadrant.UpperLeft, position, upperOrder++));

        var lowerOrder = 17;
        foreach (var position in Enumerable.Range(1, 8).Reverse())   // LR8 .. LR1
            teeth.Add(Permanent(40 + position, Quadrant.LowerRight, position, lowerOrder++));
        foreach (var position in Enumerable.Range(1, 8))             // LL1 .. LL8
            teeth.Add(Permanent(30 + position, Quadrant.LowerLeft, position, lowerOrder++));

        // ---- primary dentition, quadrants 5-8 -----------------------------
        var upperPrimaryOrder = 101;
        foreach (var position in Enumerable.Range(1, 5).Reverse())
            teeth.Add(Primary(50 + position, Quadrant.UpperRightPrimary, position, upperPrimaryOrder++));
        foreach (var position in Enumerable.Range(1, 5))
            teeth.Add(Primary(60 + position, Quadrant.UpperLeftPrimary, position, upperPrimaryOrder++));

        var lowerPrimaryOrder = 111;
        foreach (var position in Enumerable.Range(1, 5).Reverse())
            teeth.Add(Primary(80 + position, Quadrant.LowerRightPrimary, position, lowerPrimaryOrder++));
        foreach (var position in Enumerable.Range(1, 5))
            teeth.Add(Primary(70 + position, Quadrant.LowerLeftPrimary, position, lowerPrimaryOrder++));

        return teeth;
    }

    private static Tooth Permanent(int fdi, Quadrant quadrant, int position, int chartOrder)
    {
        var arch = quadrant is Quadrant.UpperRight or Quadrant.UpperLeft ? DentalArch.Upper : DentalArch.Lower;
        var type = position switch
        {
            1 or 2 => ToothType.Incisor,
            3 => ToothType.Canine,
            4 or 5 => ToothType.Premolar,
            _ => ToothType.Molar
        };

        var posterior = type is ToothType.Premolar or ToothType.Molar;
        var (roots, canals) = PermanentRootAnatomy(arch, position);

        return new Tooth
        {
            Id = IdFor(fdi),
            FdiNumber = fdi,
            UniversalNumber = PermanentUniversal(fdi).ToString(),
            PalmerNotation = Palmer(quadrant, position),
            Name = $"{QuadrantWords(quadrant)} {PermanentNames[position]}",
            ShortName = $"{Palmer(quadrant, position)}",
            Arch = arch,
            Quadrant = quadrant,
            ToothType = type,
            IsPrimary = false,
            PositionInQuadrant = position,
            RootCount = roots,
            CanalCount = canals,
            HasOcclusalSurface = posterior,
            ValidSurfaces = Surfaces(arch, posterior),
            ChartOrder = chartOrder
        };
    }

    private static Tooth Primary(int fdi, Quadrant quadrant, int position, int chartOrder)
    {
        var arch = quadrant is Quadrant.UpperRightPrimary or Quadrant.UpperLeftPrimary
            ? DentalArch.Upper : DentalArch.Lower;

        var type = position switch
        {
            1 or 2 => ToothType.Incisor,
            3 => ToothType.Canine,
            _ => ToothType.Molar
        };

        var posterior = type == ToothType.Molar;

        return new Tooth
        {
            Id = IdFor(fdi),
            FdiNumber = fdi,
            UniversalNumber = PrimaryUniversal(fdi),
            PalmerNotation = Palmer(quadrant, position),
            Name = $"{QuadrantWords(quadrant)} Primary {PrimaryNames[position]}",
            ShortName = Palmer(quadrant, position),
            Arch = arch,
            Quadrant = quadrant,
            ToothType = type,
            IsPrimary = true,
            PositionInQuadrant = position,
            RootCount = posterior ? (arch == DentalArch.Upper ? 3 : 2) : 1,
            CanalCount = posterior ? (arch == DentalArch.Upper ? 3 : 3) : 1,
            HasOcclusalSurface = posterior,
            ValidSurfaces = Surfaces(arch, posterior),
            ChartOrder = chartOrder
        };
    }

    private static ToothSurface Surfaces(DentalArch arch, bool posterior)
    {
        var surfaces = ToothSurface.Mesial | ToothSurface.Distal | ToothSurface.Buccal |
                       ToothSurface.Cervical | ToothSurface.Root;

        surfaces |= arch == DentalArch.Upper ? ToothSurface.Palatal : ToothSurface.Lingual;
        surfaces |= posterior ? ToothSurface.Occlusal : ToothSurface.Incisal;
        return surfaces;
    }

    /// <summary>Typical root and canal counts. Real anatomy varies; these are defaults.</summary>
    private static (int Roots, int Canals) PermanentRootAnatomy(DentalArch arch, int position) =>
        (arch, position) switch
        {
            (DentalArch.Upper, 1) => (1, 1),
            (DentalArch.Upper, 2) => (1, 1),
            (DentalArch.Upper, 3) => (1, 1),
            (DentalArch.Upper, 4) => (2, 2),
            (DentalArch.Upper, 5) => (1, 2),
            (DentalArch.Upper, 6) => (3, 4),
            (DentalArch.Upper, 7) => (3, 3),
            (DentalArch.Upper, 8) => (3, 3),
            (DentalArch.Lower, 1) => (1, 1),
            (DentalArch.Lower, 2) => (1, 1),
            (DentalArch.Lower, 3) => (1, 1),
            (DentalArch.Lower, 4) => (1, 1),
            (DentalArch.Lower, 5) => (1, 1),
            (DentalArch.Lower, 6) => (2, 3),
            (DentalArch.Lower, 7) => (2, 3),
            _ => (2, 3)
        };

    /// <summary>Universal numbering: 1-16 across the upper arch, 17-32 across the lower.</summary>
    private static int PermanentUniversal(int fdi)
    {
        var quadrant = fdi / 10;
        var position = fdi % 10;
        return quadrant switch
        {
            1 => 9 - position,        // 18 -> 1  .. 11 -> 8
            2 => 8 + position,        // 21 -> 9  .. 28 -> 16
            3 => 24 + position,       // 31 -> 25 .. 38 -> 32
            4 => 25 - position,       // 48 -> 17 .. 41 -> 24
            _ => 0
        };
    }

    /// <summary>Universal lettering for the primary dentition: A-J upper, K-T lower.</summary>
    private static string PrimaryUniversal(int fdi)
    {
        var quadrant = fdi / 10;
        var position = fdi % 10;
        var index = quadrant switch
        {
            5 => 6 - position,        // 55 -> A(1) .. 51 -> E(5)
            6 => 5 + position,        // 61 -> F(6) .. 65 -> J(10)
            7 => 15 + position,       // 71 -> P(16) .. 75 -> T(20)
            8 => 16 - position,       // 85 -> K(11) .. 81 -> O(15)
            _ => 0
        };
        return index is >= 1 and <= 20 ? ((char)('A' + index - 1)).ToString() : "?";
    }

    private static string Palmer(Quadrant quadrant, int position) => quadrant switch
    {
        Quadrant.UpperRight or Quadrant.UpperRightPrimary => $"UR{position}",
        Quadrant.UpperLeft or Quadrant.UpperLeftPrimary => $"UL{position}",
        Quadrant.LowerLeft or Quadrant.LowerLeftPrimary => $"LL{position}",
        _ => $"LR{position}"
    };

    private static string QuadrantWords(Quadrant quadrant) => quadrant switch
    {
        Quadrant.UpperRight or Quadrant.UpperRightPrimary => "Upper Right",
        Quadrant.UpperLeft or Quadrant.UpperLeftPrimary => "Upper Left",
        Quadrant.LowerLeft or Quadrant.LowerLeftPrimary => "Lower Left",
        _ => "Lower Right"
    };
}
