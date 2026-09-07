using DentalSurgery.Application.Clinical;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence.Seed;
using Xunit;

namespace DentalSurgery.Tests;

public class SurfaceNotationTests
{
    [Fact]
    public void Surfaces_render_in_clinical_shorthand()
    {
        var surfaces = ToothSurface.Mesial | ToothSurface.Occlusal | ToothSurface.Distal;
        Assert.Equal("MOD", SurfaceNotation.ToCode(surfaces));
    }

    [Fact]
    public void An_empty_surface_set_renders_as_an_empty_string()
    {
        Assert.Equal(string.Empty, SurfaceNotation.ToCode(ToothSurface.None));
    }

    [Fact]
    public void The_whole_tooth_is_reported_as_whole()
    {
        Assert.Equal("Whole", SurfaceNotation.ToCode(ToothSurface.Whole));
    }

    [Theory]
    [InlineData("MO", ToothSurface.Mesial | ToothSurface.Occlusal)]
    [InlineData("DL", ToothSurface.Distal | ToothSurface.Lingual)]
    [InlineData("B", ToothSurface.Buccal)]
    public void Codes_round_trip_back_to_flags(string code, ToothSurface expected)
    {
        Assert.Equal(expected, SurfaceNotation.Parse(code));
    }

    [Fact]
    public void Parsing_is_case_insensitive()
    {
        Assert.Equal(SurfaceNotation.Parse("mod"), SurfaceNotation.Parse("MOD"));
    }

    [Fact]
    public void Splitting_lists_each_selected_surface()
    {
        var split = SurfaceNotation.Split(ToothSurface.Mesial | ToothSurface.Buccal);
        Assert.Equal(2, split.Count);
    }
}

public class DentalChartBuilderTests
{
    private readonly DentalChartBuilder _builder = new();
    private readonly List<Tooth> _teeth = ToothReferenceData.Build();
    private readonly Guid _patientId = Guid.NewGuid();

    private Tooth Tooth(int fdi) => _teeth.Single(t => t.FdiNumber == fdi);

    private ToothConditionRecord Record(
        int fdi, ToothConditionType type, ToothSurface surfaces = ToothSurface.None,
        ChartEntryStatus status = ChartEntryStatus.Existing, int daysAgo = 30) => new()
    {
        PatientId = _patientId,
        ToothId = Tooth(fdi).Id,
        ConditionType = type,
        Surfaces = surfaces,
        Status = status,
        IsActive = true,
        RecordedOn = DateOnly.FromDateTime(DateTime.Today.AddDays(-daysAgo))
    };

    [Fact]
    public void An_empty_history_gives_a_sound_dentition()
    {
        var chart = _builder.Build(_patientId, _teeth, Array.Empty<ToothConditionRecord>());

        Assert.Equal(16, chart.UpperRow.Count);
        Assert.Equal(16, chart.LowerRow.Count);
        Assert.Equal(32, chart.PresentCount);
        Assert.Equal(0, chart.MissingCount);
        Assert.Equal(0, chart.Dmft);
    }

    [Fact]
    public void The_primary_dentition_is_charted_separately()
    {
        var chart = _builder.Build(_patientId, _teeth, Array.Empty<ToothConditionRecord>(), Dentition.Primary);

        Assert.Equal(10, chart.UpperRow.Count);
        Assert.Equal(10, chart.LowerRow.Count);
        Assert.All(chart.AllTeeth, t => Assert.True(t.Tooth.IsPrimary));
    }

    [Fact]
    public void An_extracted_tooth_is_reported_as_missing()
    {
        var records = new[] { Record(46, ToothConditionType.Extracted, ToothSurface.Whole, ChartEntryStatus.Completed) };

        var chart = _builder.Build(_patientId, _teeth, records);

        Assert.True(chart.ByFdi(46)!.IsMissing);
        Assert.Equal(31, chart.PresentCount);
        Assert.Equal(1, chart.MissingCount);
    }

    [Fact]
    public void Restorations_and_caries_are_tracked_per_surface()
    {
        var records = new[]
        {
            Record(16, ToothConditionType.Restoration, ToothSurface.Mesial | ToothSurface.Occlusal),
            Record(26, ToothConditionType.Caries, ToothSurface.Distal)
        };

        var chart = _builder.Build(_patientId, _teeth, records);

        var restored = chart.ByFdi(16)!;
        Assert.True(restored.HasRestoration);
        Assert.True(restored.RestoredSurfaces.HasFlag(ToothSurface.Mesial));
        Assert.True(restored.RestoredSurfaces.HasFlag(ToothSurface.Occlusal));
        Assert.False(restored.RestoredSurfaces.HasFlag(ToothSurface.Distal));

        var carious = chart.ByFdi(26)!;
        Assert.True(carious.HasCaries);
        Assert.True(carious.CariousSurfaces.HasFlag(ToothSurface.Distal));
    }

    [Fact]
    public void Dmft_counts_decayed_missing_and_filled_permanent_teeth()
    {
        var records = new[]
        {
            Record(16, ToothConditionType.Restoration, ToothSurface.Occlusal),
            Record(26, ToothConditionType.Caries, ToothSurface.Occlusal),
            Record(36, ToothConditionType.Missing, ToothSurface.Whole),
            Record(46, ToothConditionType.Crown, ToothSurface.Whole)
        };

        var chart = _builder.Build(_patientId, _teeth, records);

        Assert.Equal(4, chart.Dmft);
    }

    [Fact]
    public void Inactive_records_are_left_out_of_the_current_chart()
    {
        var superseded = Record(16, ToothConditionType.Caries, ToothSurface.Occlusal);
        superseded.IsActive = false;

        var chart = _builder.Build(_patientId, _teeth, new[] { superseded });

        Assert.False(chart.ByFdi(16)!.HasCaries);
    }

    [Fact]
    public void The_chart_can_be_replayed_to_an_earlier_date()
    {
        var recent = Record(16, ToothConditionType.Caries, ToothSurface.Occlusal, daysAgo: 5);

        var today = _builder.Build(_patientId, _teeth, new[] { recent });
        var lastMonth = _builder.Build(_patientId, _teeth, new[] { recent },
            Dentition.Permanent, DateOnly.FromDateTime(DateTime.Today.AddDays(-20)));

        Assert.True(today.ByFdi(16)!.HasCaries);
        Assert.False(lastMonth.ByFdi(16)!.HasCaries);
    }

    [Fact]
    public void Planned_work_is_separated_from_completed_work()
    {
        var records = new[]
        {
            Record(15, ToothConditionType.Restoration, ToothSurface.Occlusal, ChartEntryStatus.Planned)
        };

        var chart = _builder.Build(_patientId, _teeth, records);
        var tooth = chart.ByFdi(15)!;

        Assert.True(tooth.HasPlannedWork);
        Assert.False(tooth.HasRestoration);
        Assert.Equal(1, chart.PlannedWorkCount);
    }

    [Fact]
    public void An_implant_is_recognised_even_though_the_tooth_was_extracted()
    {
        var records = new[]
        {
            Record(46, ToothConditionType.Extracted, ToothSurface.Whole, ChartEntryStatus.Completed, 400),
            Record(46, ToothConditionType.Implant, ToothSurface.Root, ChartEntryStatus.Completed, 200)
        };

        var chart = _builder.Build(_patientId, _teeth, records);
        var tooth = chart.ByFdi(46)!;

        Assert.True(tooth.IsImplant);
        Assert.True(tooth.IsMissing);
        Assert.Equal(1, chart.ImplantCount);
    }
}

public class PeriodontalAnalyserTests
{
    private readonly PeriodontalAnalyser _analyser = new();
    private readonly List<Tooth> _teeth = ToothReferenceData.Build().Where(t => !t.IsPrimary).ToList();

    private PeriodontalChart BuildChart(int pocketDepth, int gingivalMargin = 0, bool bleeding = false)
    {
        var chart = new PeriodontalChart { PatientId = Guid.NewGuid() };

        foreach (var tooth in _teeth.Where(t => t.PositionInQuadrant <= 7))
        {
            foreach (var site in Enum.GetValues<PeriodontalSite>())
            {
                chart.Measurements.Add(new PeriodontalMeasurement
                {
                    PeriodontalChartId = chart.Id,
                    ToothId = tooth.Id,
                    Tooth = tooth,
                    Site = site,
                    PocketDepthMm = pocketDepth,
                    GingivalMarginMm = gingivalMargin,
                    BleedingOnProbing = bleeding
                });
            }
        }

        return chart;
    }

    [Fact]
    public void An_empty_chart_reports_healthy_with_a_prompt_to_chart()
    {
        var summary = _analyser.Analyse(new PeriodontalChart());

        Assert.Equal(PeriodontalDiagnosis.Healthy, summary.SuggestedDiagnosis);
        Assert.Contains(summary.Recommendations, r => r.Contains("Complete a full-mouth"));
    }

    [Fact]
    public void Shallow_pockets_without_bleeding_are_healthy()
    {
        var summary = _analyser.Analyse(BuildChart(2));

        Assert.Equal(PeriodontalDiagnosis.Healthy, summary.SuggestedDiagnosis);
        Assert.Equal(0m, summary.BleedingPercent);
        Assert.Equal(2m, summary.MeanPocketDepth);
    }

    [Fact]
    public void Shallow_pockets_with_bleeding_are_gingivitis()
    {
        var summary = _analyser.Analyse(BuildChart(3, bleeding: true));

        Assert.Equal(PeriodontalDiagnosis.Gingivitis, summary.SuggestedDiagnosis);
        Assert.Equal(100m, summary.BleedingPercent);
    }

    [Fact]
    public void Attachment_loss_produces_a_periodontitis_stage()
    {
        // 6mm pockets with 3mm recession gives 9mm of attachment loss.
        var summary = _analyser.Analyse(BuildChart(6, gingivalMargin: -3, bleeding: true));

        Assert.True(summary.SuggestedDiagnosis >= PeriodontalDiagnosis.PeriodontitisStageIIIGradeA);
        Assert.Equal(9, summary.MaxAttachmentLoss);
        Assert.True(summary.RiskLevel >= RiskLevel.High);
    }

    [Fact]
    public void Smoking_raises_the_grade()
    {
        var chart = BuildChart(5, gingivalMargin: -1, bleeding: true);

        var nonSmoker = _analyser.Analyse(chart, 45, isSmoker: false, isDiabetic: false);
        var smoker = _analyser.Analyse(chart, 45, isSmoker: true, isDiabetic: false);

        Assert.True(smoker.SuggestedDiagnosis > nonSmoker.SuggestedDiagnosis);
        Assert.Contains(smoker.Recommendations, r => r.Contains("smoking cessation"));
    }

    [Fact]
    public void Deeper_disease_shortens_the_recommended_recall()
    {
        var mild = _analyser.Analyse(BuildChart(2));
        var severe = _analyser.Analyse(BuildChart(7, gingivalMargin: -3, bleeding: true));

        Assert.True(severe.RecommendedRecallMonths < mild.RecommendedRecallMonths);
        Assert.Equal(3, severe.RecommendedRecallMonths);
    }

    [Fact]
    public void Sites_are_bucketed_by_probing_depth()
    {
        var summary = _analyser.Analyse(BuildChart(5));

        Assert.Equal(0, summary.SitesUnder4mm);
        Assert.Equal(summary.SitesRecorded, summary.Sites4To5mm);
        Assert.Equal(0, summary.Sites6To7mm);
    }

    [Theory]
    [InlineData(2, false, false, "0")]
    [InlineData(2, true, false, "1")]
    [InlineData(2, true, true, "2")]
    [InlineData(4, false, false, "3")]
    [InlineData(6, false, false, "4")]
    public void Bpe_codes_follow_the_deepest_pocket_in_the_sextant(
        int depth, bool bleeding, bool calculus, string expected)
    {
        var tooth = _teeth.First();
        var sites = Enum.GetValues<PeriodontalSite>().Select(site => new PeriodontalMeasurement
        {
            ToothId = tooth.Id,
            Tooth = tooth,
            Site = site,
            PocketDepthMm = depth,
            BleedingOnProbing = bleeding,
            CalculusPresent = calculus
        }).ToList();

        Assert.Equal(expected, PeriodontalAnalyser.BpeCode(sites));
    }

    [Fact]
    public void Furcation_involvement_appends_an_asterisk_to_the_bpe_code()
    {
        var tooth = _teeth.First(t => t.ToothType == ToothType.Molar);
        var sites = Enum.GetValues<PeriodontalSite>().Select(site => new PeriodontalMeasurement
        {
            ToothId = tooth.Id,
            Tooth = tooth,
            Site = site,
            PocketDepthMm = 6,
            Furcation = FurcationGrade.ClassII
        }).ToList();

        Assert.Equal("4*", PeriodontalAnalyser.BpeCode(sites));
    }

    [Fact]
    public void A_sextant_with_no_teeth_is_coded_x()
    {
        Assert.Equal("X", PeriodontalAnalyser.BpeCode(Array.Empty<PeriodontalMeasurement>()));
    }

    [Fact]
    public void Missing_sites_are_excluded_from_the_indices()
    {
        var chart = BuildChart(4, bleeding: true);
        foreach (var measurement in chart.Measurements.Take(12)) measurement.IsMissing = true;

        var summary = _analyser.Analyse(chart);

        Assert.Equal(chart.Measurements.Count - 12, summary.SitesRecorded);
    }
}
