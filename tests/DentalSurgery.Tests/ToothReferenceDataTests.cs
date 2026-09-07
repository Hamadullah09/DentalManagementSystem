using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence.Seed;
using Xunit;

namespace DentalSurgery.Tests;

public class ToothReferenceDataTests
{
    private static readonly List<Domain.Entities.Tooth> Teeth = ToothReferenceData.Build();

    [Fact]
    public void Builds_thirty_two_permanent_and_twenty_primary_teeth()
    {
        Assert.Equal(32, Teeth.Count(t => !t.IsPrimary));
        Assert.Equal(20, Teeth.Count(t => t.IsPrimary));
        Assert.Equal(52, Teeth.Count);
    }

    [Fact]
    public void Fdi_numbers_are_unique()
    {
        var duplicates = Teeth.GroupBy(t => t.FdiNumber).Where(g => g.Count() > 1).ToList();
        Assert.Empty(duplicates);
    }

    [Fact]
    public void Identifiers_are_stable_across_builds()
    {
        var second = ToothReferenceData.Build();

        foreach (var tooth in Teeth)
        {
            var match = second.Single(t => t.FdiNumber == tooth.FdiNumber);
            Assert.Equal(tooth.Id, match.Id);
        }
    }

    [Theory]
    [InlineData(18, "1")]      // upper right third molar
    [InlineData(11, "8")]      // upper right central incisor
    [InlineData(21, "9")]      // upper left central incisor
    [InlineData(28, "16")]     // upper left third molar
    [InlineData(38, "32")]     // lower left third molar
    [InlineData(31, "25")]     // lower left central incisor
    [InlineData(41, "24")]     // lower right central incisor
    [InlineData(48, "17")]     // lower right third molar
    public void Universal_numbering_matches_the_fdi_position(int fdi, string expectedUniversal)
    {
        var tooth = Teeth.Single(t => t.FdiNumber == fdi);
        Assert.Equal(expectedUniversal, tooth.UniversalNumber);
    }

    [Theory]
    [InlineData(55, "A")]
    [InlineData(51, "E")]
    [InlineData(61, "F")]
    [InlineData(65, "J")]
    [InlineData(85, "K")]
    [InlineData(81, "O")]
    [InlineData(71, "P")]
    [InlineData(75, "T")]
    public void Primary_teeth_use_universal_lettering(int fdi, string expectedLetter)
    {
        var tooth = Teeth.Single(t => t.FdiNumber == fdi);
        Assert.Equal(expectedLetter, tooth.UniversalNumber);
    }

    [Theory]
    [InlineData(16, ToothType.Molar, true)]
    [InlineData(14, ToothType.Premolar, true)]
    [InlineData(13, ToothType.Canine, false)]
    [InlineData(12, ToothType.Incisor, false)]
    public void Tooth_type_and_occlusal_surface_follow_the_position(int fdi, ToothType type, bool hasOcclusal)
    {
        var tooth = Teeth.Single(t => t.FdiNumber == fdi);
        Assert.Equal(type, tooth.ToothType);
        Assert.Equal(hasOcclusal, tooth.HasOcclusalSurface);
    }

    [Fact]
    public void Upper_teeth_have_a_palatal_surface_and_lower_teeth_a_lingual_one()
    {
        var upper = Teeth.Single(t => t.FdiNumber == 16);
        var lower = Teeth.Single(t => t.FdiNumber == 46);

        Assert.True(upper.ValidSurfaces.HasFlag(ToothSurface.Palatal));
        Assert.False(upper.ValidSurfaces.HasFlag(ToothSurface.Lingual));

        Assert.True(lower.ValidSurfaces.HasFlag(ToothSurface.Lingual));
        Assert.False(lower.ValidSurfaces.HasFlag(ToothSurface.Palatal));
    }

    [Fact]
    public void Chart_order_runs_left_to_right_across_each_arch()
    {
        var upper = Teeth.Where(t => !t.IsPrimary && t.Arch == DentalArch.Upper)
            .OrderBy(t => t.ChartOrder).Select(t => t.FdiNumber).ToArray();

        Assert.Equal(new[] { 18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28 }, upper);

        var lower = Teeth.Where(t => !t.IsPrimary && t.Arch == DentalArch.Lower)
            .OrderBy(t => t.ChartOrder).Select(t => t.FdiNumber).ToArray();

        Assert.Equal(new[] { 48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38 }, lower);
    }

    [Fact]
    public void Molars_have_more_canals_than_incisors()
    {
        var upperFirstMolar = Teeth.Single(t => t.FdiNumber == 16);
        var upperCentral = Teeth.Single(t => t.FdiNumber == 11);

        Assert.True(upperFirstMolar.CanalCount > upperCentral.CanalCount);
        Assert.Equal(1, upperCentral.CanalCount);
    }
}
