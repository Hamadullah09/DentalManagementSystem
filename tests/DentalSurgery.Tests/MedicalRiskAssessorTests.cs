using DentalSurgery.Application.Clinical;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using Xunit;

namespace DentalSurgery.Tests;

public class MedicalRiskAssessorTests
{
    private readonly MedicalRiskAssessor _assessor = new();

    private static Patient MakePatient(int age = 40) => new()
    {
        Name = new PersonName { FirstName = "Test", LastName = "Patient" },
        DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-age))
    };

    [Fact]
    public void A_healthy_patient_produces_no_flags()
    {
        var profile = _assessor.Assess(MakePatient(), [], [], []);

        Assert.False(profile.HasAnyFlags);
        Assert.Equal(RiskLevel.None, profile.OverallLevel);
        Assert.Equal(AsaClassification.AsaI, profile.EstimatedAsa);
        Assert.False(profile.BleedingRisk);
        Assert.False(profile.RequiresAntibioticProphylaxis);
    }

    [Fact]
    public void An_anaphylactic_allergy_is_critical()
    {
        var allergies = new List<PatientAllergy>
        {
            new()
            {
                FreeTextAllergen = "Penicillin",
                Severity = AllergySeverity.Anaphylaxis,
                Reaction = "Airway compromise",
                IsActive = true
            }
        };

        var profile = _assessor.Assess(MakePatient(), allergies, [], []);

        Assert.Equal(RiskLevel.Critical, profile.OverallLevel);
        Assert.Contains(profile.Flags, f => f.Title.Contains("Penicillin"));
    }

    [Fact]
    public void A_latex_allergy_raises_the_latex_free_protocol()
    {
        var allergies = new List<PatientAllergy>
        {
            new() { FreeTextAllergen = "Latex", AllergyType = AllergyType.Latex, Severity = AllergySeverity.Moderate, IsActive = true }
        };

        var profile = _assessor.Assess(MakePatient(), allergies, [], []);

        Assert.True(profile.LatexAllergy);
        Assert.Contains(profile.Flags, f => f.Code == "LATEX");
    }

    [Theory]
    [InlineData("Warfarin 3mg")]
    [InlineData("Apixaban 5mg")]
    [InlineData("Clopidogrel 75mg")]
    [InlineData("Aspirin 75mg")]
    public void Anticoagulants_raise_the_bleeding_risk(string medicationName)
    {
        var medications = new List<PatientMedication>
        {
            new() { FreeTextMedication = medicationName, IsCurrent = true }
        };

        var profile = _assessor.Assess(MakePatient(), [], [], medications);

        Assert.True(profile.BleedingRisk);
        Assert.Contains(profile.Flags, f => f.Code.StartsWith("MED:ANTICOAG"));
    }

    [Theory]
    [InlineData("Alendronic acid 70mg")]
    [InlineData("Zoledronic acid")]
    [InlineData("Denosumab")]
    public void Antiresorptives_raise_the_mronj_flag(string medicationName)
    {
        var medications = new List<PatientMedication>
        {
            new() { FreeTextMedication = medicationName, IsCurrent = true }
        };

        var profile = _assessor.Assess(MakePatient(), [], [], medications);

        Assert.True(profile.MronjRisk);
        Assert.Contains(profile.Flags, f => f.Code.StartsWith("MED:BISPHOS"));
    }

    [Fact]
    public void A_history_of_endocarditis_requires_prophylaxis()
    {
        var review = new MedicalHistoryReview { HistoryOfEndocarditis = true };

        var profile = _assessor.Assess(MakePatient(), [], [], [], review);

        Assert.True(profile.RequiresAntibioticProphylaxis);
        Assert.Equal(RiskLevel.Critical, profile.OverallLevel);
        Assert.Contains(profile.Flags, f => f.Code == "ENDOCARDITIS");
    }

    [Fact]
    public void Pregnancy_is_flagged_with_the_gestation_when_known()
    {
        var review = new MedicalHistoryReview { IsPregnant = true, WeeksPregnant = 22 };

        var profile = _assessor.Assess(MakePatient(30), [], [], [], review);

        var flag = Assert.Single(profile.Flags, f => f.Code == "PREGNANCY");
        Assert.Contains("22 weeks", flag.Detail);
    }

    [Fact]
    public void A_condition_that_contraindicates_adrenaline_sets_the_caution()
    {
        var conditions = new List<PatientMedicalCondition>
        {
            new()
            {
                Status = ConditionStatus.Chronic,
                Severity = AlertSeverity.High,
                MedicalCondition = new MedicalCondition
                {
                    Code = "IHD",
                    Name = "Ischaemic heart disease",
                    ContraindicatesAdrenaline = true,
                    AffectsAnaesthesia = true
                }
            }
        };

        var profile = _assessor.Assess(MakePatient(65), [], conditions, []);

        Assert.True(profile.AdrenalineCaution);
        Assert.True(profile.SedationCaution);
        Assert.Contains(profile.Flags, f => f.Code == "ADRENALINE");
    }

    [Fact]
    public void Duplicate_flags_are_collapsed_to_the_most_severe()
    {
        // The same allergy recorded twice must not produce two banner entries.
        var allergies = new List<PatientAllergy>
        {
            new() { FreeTextAllergen = "Penicillin", Severity = AllergySeverity.Mild, IsActive = true },
            new() { FreeTextAllergen = "Penicillin", Severity = AllergySeverity.Severe, IsActive = true }
        };

        var profile = _assessor.Assess(MakePatient(), allergies, [], []);

        var penicillinFlags = profile.Flags.Where(f => f.Code.Contains("Penicillin")).ToList();
        Assert.Single(penicillinFlags);
        Assert.Equal(RiskLevel.High, penicillinFlags[0].Level);
    }

    [Fact]
    public void The_review_asa_classification_takes_priority_over_the_estimate()
    {
        var review = new MedicalHistoryReview { AsaClassification = AsaClassification.AsaIV };

        var profile = _assessor.Assess(MakePatient(), [], [], [], review);

        Assert.Equal(AsaClassification.AsaIV, profile.EstimatedAsa);
        Assert.True(profile.SedationCaution);
    }

    [Fact]
    public void Elderly_patients_get_a_low_level_advisory()
    {
        var profile = _assessor.Assess(MakePatient(80), [], [], []);
        Assert.Contains(profile.Flags, f => f.Code == "AGE");
    }
}
