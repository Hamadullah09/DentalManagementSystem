using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Claims;
using DentalSurgery.Infrastructure.Notifications;
using DentalSurgery.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DentalSurgery.Tests;

public class X12ClaimGeneratorTests
{
    private static readonly DateTime Generated = new(2026, 9, 6, 14, 30, 0, DateTimeKind.Utc);

    private readonly X12ClaimGenerator _generator = new(Options.Create(new ClaimSubmissionOptions
    {
        InterchangeSenderId = "MERIDIAN",
        InterchangeReceiverId = "CLRHOUSE",
        SubmitterId = "MERIDIANDENTAL",
        SubmitterName = "MERIDIAN DENTAL SURGERY",
        BillingProviderId = "1234567893",
        BillingProviderTaxId = "09482716",
        UsageIndicator = "T"
    }));

    private static readonly List<Tooth> Teeth = ToothReferenceData.Build();

    private static Practice MakePractice() => new()
    {
        Name = "Meridian Dental Surgery",
        LegalName = "Meridian Dental Care Limited",
        Address = new Address { Line1 = "42 Harley Mews", City = "London", PostCode = "W1G 8QT" }
    };

    private static InsuranceClaim MakeClaim(
        bool subscriberIsPatient = true,
        bool withTooth = true,
        int lineCount = 1,
        string? payerId = "BUPA-DEN")
    {
        var tooth = Teeth.First(t => t.FdiNumber == 16);

        var claim = new InsuranceClaim
        {
            ClaimNumber = "CLM-2026-000042",
            ServiceDate = new DateOnly(2026, 9, 1),
            TotalCharged = 155m * lineCount,
            Patient = new Patient
            {
                PatientNumber = "P-000026",
                Name = new PersonName { FirstName = "Bertrand", LastName = "Abernathy" },
                DateOfBirth = new DateOnly(1958, 12, 14),
                Gender = Gender.Male,
                Address = new Address { Line1 = "12 Wentworth Gardens", City = "London", PostCode = "N7 9DP" }
            },
            Provider = new Staff
            {
                Name = new PersonName { FirstName = "Amara", LastName = "Okonkwo" },
                RegistrationNumber = "GDC-200137"
            },
            PatientInsurance = new PatientInsurance
            {
                MemberId = "88123456",
                Priority = InsurancePriority.Primary,
                SubscriberIsPatient = subscriberIsPatient,
                SubscriberName = subscriberIsPatient
                    ? null
                    : new PersonName { FirstName = "Marjorie", LastName = "Abernathy" },
                SubscriberDateOfBirth = subscriberIsPatient ? null : new DateOnly(1960, 3, 2),
                RelationshipToSubscriber = subscriberIsPatient
                    ? ContactRelationship.Unknown
                    : ContactRelationship.Spouse,
                EffectiveFrom = new DateOnly(2020, 1, 1),
                InsurancePlan = new InsurancePlan
                {
                    PlanName = "Bupa Dental Level 3",
                    GroupNumber = "GRP-9021",
                    InsuranceCarrier = new InsuranceCarrier
                    {
                        Name = "Bupa Dental Insurance",
                        ElectronicPayerId = payerId,
                        Address = new Address { Line1 = "Bupa House", City = "London", PostCode = "WC1A 2BA" }
                    }
                }
            }
        };

        for (var i = 1; i <= lineCount; i++)
        {
            claim.Lines.Add(new InsuranceClaimLine
            {
                Sequence = i,
                ServiceDate = new DateOnly(2026, 9, 1),
                ChargedAmount = 155m,
                AllowedAmount = 124m,
                SurfaceCode = withTooth ? "MOD" : null,
                ToothId = withTooth ? tooth.Id : null,
                Tooth = withTooth ? tooth : null,
                ProcedureCode = new ProcedureCode
                {
                    Code = "D2392",
                    ShortDescription = "Composite - two surfaces, posterior",
                    Category = ProcedureCategory.Restorative,
                    RequiresTooth = true
                }
            });
        }

        return claim;
    }

    // ---------------------------------------------------------------- validation

    [Fact]
    public void A_complete_claim_passes_validation()
    {
        Assert.Empty(_generator.Validate(MakeClaim()));
    }

    [Fact]
    public void A_claim_with_no_lines_is_rejected()
    {
        var claim = MakeClaim();
        claim.Lines.Clear();

        var issues = _generator.Validate(claim);
        Assert.Contains(issues, i => i.Field == "Lines" && i.IsBlocking);
    }

    [Fact]
    public void A_missing_date_of_birth_is_blocking()
    {
        var claim = MakeClaim();
        claim.Patient!.DateOfBirth = null;

        Assert.Contains(_generator.Validate(claim), i => i.Field == "Patient.DateOfBirth" && i.IsBlocking);
    }

    [Fact]
    public void A_carrier_without_a_payer_identifier_is_rejected()
    {
        var claim = MakeClaim(payerId: null);
        claim.PatientInsurance!.InsurancePlan!.InsuranceCarrier!.PayerId = null;

        Assert.Contains(_generator.Validate(claim), i => i.Field == "Carrier.PayerId" && i.IsBlocking);
    }

    [Fact]
    public void A_missing_member_number_is_rejected()
    {
        var claim = MakeClaim();
        claim.PatientInsurance!.MemberId = null;
        claim.PatientInsurance.PolicyNumber = null;

        Assert.Contains(_generator.Validate(claim), i => i.Field == "Insurance.MemberId" && i.IsBlocking);
    }

    [Fact]
    public void A_zero_charge_line_is_rejected()
    {
        var claim = MakeClaim();
        claim.Lines.First().ChargedAmount = 0m;

        Assert.Contains(_generator.Validate(claim), i => i.Message.Contains("greater than zero"));
    }

    [Fact]
    public void A_tooth_specific_code_without_a_tooth_warns_but_does_not_block()
    {
        var claim = MakeClaim(withTooth: false);

        var issue = Assert.Single(_generator.Validate(claim), i => i.Message.Contains("tooth-specific"));
        Assert.False(issue.IsBlocking);
    }

    // ---------------------------------------------------------------- structure

    [Fact]
    public void The_interchange_opens_with_isa_and_closes_with_iea()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);

        Assert.StartsWith("ISA*", interchange);
        Assert.Contains("IEA*1*", interchange);
    }

    [Fact]
    public void Envelope_control_numbers_match_between_the_header_and_the_trailer()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);
        var segments = Segments(interchange);

        var isa = segments.First(s => s.StartsWith("ISA*")).Split('*');
        var iea = segments.First(s => s.StartsWith("IEA*")).Split('*');
        var gs = segments.First(s => s.StartsWith("GS*")).Split('*');
        var ge = segments.First(s => s.StartsWith("GE*")).Split('*');

        Assert.Equal(isa[13], iea[2]);
        Assert.Equal(gs[6], ge[2]);
    }

    [Fact]
    public void The_isa_segment_has_the_sixteen_elements_the_standard_requires()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);
        var isa = Segments(interchange).First(s => s.StartsWith("ISA*")).Split('*');

        // The tag plus sixteen elements.
        Assert.Equal(17, isa.Length);
        Assert.Equal(15, isa[6].Length);   // sender id is fixed width
        Assert.Equal(15, isa[8].Length);   // receiver id is fixed width
        Assert.Equal("00501", isa[12]);
        Assert.Equal("T", isa[15]);
    }

    [Fact]
    public void The_transaction_declares_the_dental_implementation_guide()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);

        Assert.Contains("ST*837*0001*005010X224A2", interchange);
        Assert.Contains("GS*HC*", interchange);
    }

    [Fact]
    public void The_se_segment_counts_the_transaction_segments_including_itself()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);
        var segments = Segments(interchange);

        var stIndex = segments.FindIndex(s => s.StartsWith("ST*"));
        var seIndex = segments.FindIndex(s => s.StartsWith("SE*"));
        var declared = int.Parse(segments[seIndex].Split('*')[1]);

        Assert.Equal(seIndex - stIndex + 1, declared);
    }

    [Fact]
    public void A_claim_carries_the_charge_and_the_service_date()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);

        Assert.Contains("CLM*CLM-2026-000042*155.00", interchange);
        Assert.Contains("DTP*472*D8*20260901", interchange);
    }

    [Fact]
    public void Procedure_codes_are_qualified_as_dental()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);
        Assert.Contains("SV3*AD:D2392*155.00", interchange);
    }

    [Fact]
    public void The_tooth_and_its_surfaces_are_reported()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);

        // JP is the FDI/international tooth numbering qualifier.
        Assert.Contains("TOO*JP*16*M:O:D", interchange);
    }

    [Fact]
    public void Each_service_line_gets_its_own_lx_and_sv3()
    {
        var interchange = _generator.Build(MakeClaim(lineCount: 3), MakePractice(), Generated);
        var segments = Segments(interchange);

        Assert.Equal(3, segments.Count(s => s.StartsWith("LX*")));
        Assert.Equal(3, segments.Count(s => s.StartsWith("SV3*")));
        Assert.Contains("LX*3", interchange);
    }

    [Fact]
    public void When_the_patient_is_the_subscriber_there_is_no_separate_patient_loop()
    {
        var interchange = _generator.Build(MakeClaim(subscriberIsPatient: true), MakePractice(), Generated);

        Assert.Contains("SBR*P*18*", interchange);
        Assert.DoesNotContain("NM1*QC*", interchange);
        Assert.Equal(2, Segments(interchange).Count(s => s.StartsWith("HL*")));
    }

    [Fact]
    public void When_the_patient_is_a_dependant_a_patient_loop_is_added()
    {
        var interchange = _generator.Build(MakeClaim(subscriberIsPatient: false), MakePractice(), Generated);

        Assert.Contains("NM1*QC*1*ABERNATHY*BERTRAND", interchange);
        Assert.Contains("NM1*IL*1*ABERNATHY*MARJORIE", interchange);
        Assert.Contains("PAT*01", interchange);
        Assert.Equal(3, Segments(interchange).Count(s => s.StartsWith("HL*")));
    }

    [Fact]
    public void Names_are_upper_cased_as_trading_partners_expect()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);
        Assert.Contains("ABERNATHY*BERTRAND", interchange);
    }

    [Fact]
    public void Postcodes_are_written_without_spaces()
    {
        var interchange = _generator.Build(MakeClaim(), MakePractice(), Generated);

        Assert.Contains("N79DP", interchange);
        Assert.DoesNotContain("N7 9DP", interchange);
    }

    [Fact]
    public void Delimiters_inside_patient_data_cannot_corrupt_the_interchange()
    {
        var claim = MakeClaim();
        // A surname containing the element and segment separators.
        claim.Patient!.Name.LastName = "O*Brien~Smith";

        var interchange = _generator.Build(claim, MakePractice(), Generated);
        var segments = Segments(interchange);

        Assert.Contains("O BRIEN SMITH", interchange);

        // The segment count must still balance despite the hostile input.
        var stIndex = segments.FindIndex(s => s.StartsWith("ST*"));
        var seIndex = segments.FindIndex(s => s.StartsWith("SE*"));
        Assert.Equal(seIndex - stIndex + 1, int.Parse(segments[seIndex].Split('*')[1]));
    }

    [Fact]
    public void A_pre_authorisation_uses_the_reporting_transaction_purpose()
    {
        var claim = MakeClaim();
        claim.IsPreAuthorisation = true;

        var interchange = _generator.Build(claim, MakePractice(), Generated);
        Assert.Contains("*RP~", interchange);
    }

    [Fact]
    public void The_same_claim_produces_the_same_control_number_so_resubmissions_are_recognisable()
    {
        var first = _generator.Build(MakeClaim(), MakePractice(), Generated);
        var second = _generator.Build(MakeClaim(), MakePractice(), Generated.AddHours(3));

        var firstIsa = Segments(first).First(s => s.StartsWith("ISA*")).Split('*')[13];
        var secondIsa = Segments(second).First(s => s.StartsWith("ISA*")).Split('*')[13];

        Assert.Equal(firstIsa, secondIsa);
        Assert.Equal(9, firstIsa.Length);
    }

    private static List<string> Segments(string interchange) =>
        interchange.Split('~', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
}

public class ClaimGatewayTests
{
    [Fact]
    public async Task The_file_drop_gateway_writes_the_outbound_and_archive_copies()
    {
        var root = Path.Combine(Path.GetTempPath(), "dental-claim-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new ClaimSubmissionOptions
        {
            OutboundFolder = Path.Combine(root, "outbound"),
            ArchiveFolder = Path.Combine(root, "archive")
        });

        var gateway = new FileDropClaimGateway(options, NullLogger<FileDropClaimGateway>.Instance);

        try
        {
            var result = await gateway.SubmitAsync("CLM-2026-000042", "ISA*00*~IEA*1*000000001~");

            Assert.True(result.Accepted);
            Assert.Single(Directory.GetFiles(options.Value.OutboundFolder));
            Assert.Single(Directory.GetFiles(options.Value.ArchiveFolder));
            Assert.NotNull(result.StoredAtPath);
            Assert.True(File.Exists(result.StoredAtPath!));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task A_claim_number_with_unsafe_characters_still_produces_a_valid_file_name()
    {
        var root = Path.Combine(Path.GetTempPath(), "dental-claim-tests", Guid.NewGuid().ToString("N"));
        var options = Options.Create(new ClaimSubmissionOptions
        {
            OutboundFolder = Path.Combine(root, "outbound"),
            ArchiveFolder = Path.Combine(root, "archive")
        });

        var gateway = new FileDropClaimGateway(options, NullLogger<FileDropClaimGateway>.Instance);

        try
        {
            var result = await gateway.SubmitAsync("CLM/2026\\42:test", "ISA*00*~");

            Assert.True(result.Accepted);
            var written = Directory.GetFiles(options.Value.OutboundFolder).Single();
            Assert.DoesNotContain('/', Path.GetFileName(written));
            Assert.DoesNotContain('\\', Path.GetFileName(written));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void The_selector_falls_back_to_manual_when_http_is_not_configured()
    {
        var options = Options.Create(new ClaimSubmissionOptions { Mode = "Http", Endpoint = null });

        var selector = new ClaimGatewaySelector(
            new FileDropClaimGateway(options, NullLogger<FileDropClaimGateway>.Instance),
            new HttpClaimGateway(new StubHttpClientFactory(), options, NullLogger<HttpClaimGateway>.Instance),
            new ManualClaimGateway(NullLogger<ManualClaimGateway>.Instance),
            options);

        Assert.Equal("Manual", selector.Resolve().Name);
    }

    [Fact]
    public void The_selector_uses_file_drop_when_configured()
    {
        var options = Options.Create(new ClaimSubmissionOptions
        {
            Mode = "FileDrop",
            OutboundFolder = "App_Data/claims/outbound"
        });

        var selector = new ClaimGatewaySelector(
            new FileDropClaimGateway(options, NullLogger<FileDropClaimGateway>.Instance),
            new HttpClaimGateway(new StubHttpClientFactory(), options, NullLogger<HttpClaimGateway>.Instance),
            new ManualClaimGateway(NullLogger<ManualClaimGateway>.Instance),
            options);

        Assert.Equal("File drop", selector.Resolve().Name);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}

