using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using System.Text;

namespace DentalSurgery.Infrastructure.Claims;

/// <summary>Problems that would cause a payer to reject the claim on receipt.</summary>
public record ClaimValidationIssue(string Field, string Message, bool IsBlocking = true);

/// <summary>
/// Builds an ANSI X12 837D (dental) interchange for a claim.
///
/// The structure follows the 005010X224A2 implementation guide: an ISA/GS
/// envelope, one ST transaction with a BHT header, the submitter and receiver
/// in loop 1000, the billing provider in 2000A, the subscriber in 2000B, the
/// patient in 2000C when they are not the subscriber, and one 2300 claim loop
/// with 2400 service lines. Trading partners each have their own companion
/// guide, so the identifiers come from configuration.
/// </summary>
public class X12ClaimGenerator(IOptions<ClaimSubmissionOptions> options)
{
    private const char ElementSeparator = '*';
    private const char CompositeSeparator = ':';
    private const string SegmentTerminator = "~\n";

    private ClaimSubmissionOptions Config => options.Value;

    /// <summary>Checks the claim carries everything the format requires.</summary>
    public IReadOnlyList<ClaimValidationIssue> Validate(InsuranceClaim claim)
    {
        var issues = new List<ClaimValidationIssue>();

        var insurance = claim.PatientInsurance;
        var plan = insurance?.InsurancePlan;
        var carrier = plan?.InsuranceCarrier;
        var patient = claim.Patient;

        if (patient is null)
            issues.Add(new ClaimValidationIssue("Patient", "The claim is not linked to a patient."));
        else
        {
            if (string.IsNullOrWhiteSpace(patient.Name.LastName))
                issues.Add(new ClaimValidationIssue("Patient.LastName", "The patient has no surname recorded."));
            if (patient.DateOfBirth is null)
                issues.Add(new ClaimValidationIssue("Patient.DateOfBirth", "A date of birth is required on every claim."));
            if (string.IsNullOrWhiteSpace(patient.Address.PostCode))
                issues.Add(new ClaimValidationIssue("Patient.Address", "A postcode is required for the subscriber address."));
        }

        if (insurance is null)
            issues.Add(new ClaimValidationIssue("Insurance", "The claim has no insurance policy attached."));
        else if (string.IsNullOrWhiteSpace(insurance.MemberId) && string.IsNullOrWhiteSpace(insurance.PolicyNumber))
            issues.Add(new ClaimValidationIssue("Insurance.MemberId", "A member or policy number is required."));

        if (carrier is null)
            issues.Add(new ClaimValidationIssue("Carrier", "The insurance plan has no carrier."));
        else if (string.IsNullOrWhiteSpace(carrier.ElectronicPayerId) && string.IsNullOrWhiteSpace(carrier.PayerId))
            issues.Add(new ClaimValidationIssue("Carrier.PayerId", "The carrier has no payer identifier for electronic claims."));

        if (claim.Lines.Count == 0)
            issues.Add(new ClaimValidationIssue("Lines", "The claim has no service lines."));

        foreach (var line in claim.Lines)
        {
            if (line.ProcedureCode is null)
                issues.Add(new ClaimValidationIssue($"Line {line.Sequence}", "The service line has no procedure code."));
            if (line.ChargedAmount <= 0)
                issues.Add(new ClaimValidationIssue($"Line {line.Sequence}", "The charged amount must be greater than zero."));
            if (line.ProcedureCode?.RequiresTooth == true && line.ToothId is null)
                issues.Add(new ClaimValidationIssue($"Line {line.Sequence}",
                    $"{line.ProcedureCode.Code} is tooth-specific and needs a tooth number.", false));
        }

        if (string.IsNullOrWhiteSpace(Config.BillingProviderId))
            issues.Add(new ClaimValidationIssue("Configuration", "No billing provider identifier is configured."));

        return issues;
    }

    /// <summary>Renders the claim as an 837D interchange.</summary>
    public string Build(InsuranceClaim claim, Practice? practice, DateTime generatedAtUtc)
    {
        var builder = new StringBuilder();
        var controlNumber = ControlNumber(claim);
        var groupControl = controlNumber;
        var transactionControl = "0001";

        var patient = claim.Patient!;
        var insurance = claim.PatientInsurance!;
        var plan = insurance.InsurancePlan!;
        var carrier = plan.InsuranceCarrier!;
        var payerId = Blank(carrier.ElectronicPayerId) ?? carrier.PayerId ?? "PAYERID";

        var subscriberIsPatient = insurance.SubscriberIsPatient;
        var subscriberLast = subscriberIsPatient ? patient.Name.LastName : insurance.SubscriberName?.LastName ?? patient.Name.LastName;
        var subscriberFirst = subscriberIsPatient ? patient.Name.FirstName : insurance.SubscriberName?.FirstName ?? patient.Name.FirstName;
        var memberId = Blank(insurance.MemberId) ?? insurance.PolicyNumber ?? "UNKNOWN";

        // ---- interchange envelope -------------------------------------------
        Segment(builder, "ISA",
            "00", El.Raw(new string(' ', 10)),
            "00", El.Raw(new string(' ', 10)),
            "ZZ", Pad(Config.InterchangeSenderId, 15),
            "ZZ", Pad(Config.InterchangeReceiverId, 15),
            generatedAtUtc.ToString("yyMMdd"),
            generatedAtUtc.ToString("HHmm"),
            "^", "00501", controlNumber, "0",
            Config.UsageIndicator, El.Raw(CompositeSeparator.ToString()));

        Segment(builder, "GS", "HC", Config.InterchangeSenderId, Config.InterchangeReceiverId,
            generatedAtUtc.ToString("yyyyMMdd"), generatedAtUtc.ToString("HHmm"),
            groupControl, "X", "005010X224A2");

        Segment(builder, "ST", "837", transactionControl, "005010X224A2");

        Segment(builder, "BHT", "0019", "00", claim.ClaimNumber,
            generatedAtUtc.ToString("yyyyMMdd"), generatedAtUtc.ToString("HHmm"),
            claim.IsPreAuthorisation ? "RP" : "CH");

        // ---- loop 1000A submitter --------------------------------------------
        Segment(builder, "NM1", "41", "2", Config.SubmitterName, "", "", "", "", "46", Config.SubmitterId);
        Segment(builder, "PER", "IC", Config.SubmitterContact, "TE", Digits(Config.SubmitterPhone));

        // ---- loop 1000B receiver ---------------------------------------------
        Segment(builder, "NM1", "40", "2", carrier.Name.ToUpperInvariant(), "", "", "", "", "46", payerId);

        // ---- loop 2000A billing provider -------------------------------------
        Segment(builder, "HL", "1", "", "20", "1");
        Segment(builder, "NM1", "85", "2",
            (practice?.LegalName ?? practice?.Name ?? Config.SubmitterName).ToUpperInvariant(),
            "", "", "", "", "XX", Config.BillingProviderId);
        Segment(builder, "N3", Upper(practice?.Address.Line1) ?? "UNKNOWN");
        Segment(builder, "N4", Upper(practice?.Address.City) ?? "UNKNOWN", "", PostCode(practice?.Address.PostCode));
        Segment(builder, "REF", "EI", Config.BillingProviderTaxId);

        // ---- loop 2000B subscriber -------------------------------------------
        Segment(builder, "HL", "2", "1", "22", subscriberIsPatient ? "0" : "1");
        Segment(builder, "SBR", PriorityCode(insurance.Priority),
            subscriberIsPatient ? "18" : RelationshipCode(insurance.RelationshipToSubscriber),
            Blank(insurance.InsurancePlan?.GroupNumber) ?? "", "", "", "", "", "", "CI");

        Segment(builder, "NM1", "IL", "1", Upper(subscriberLast), Upper(subscriberFirst), "", "", "", "MI", memberId);

        if (subscriberIsPatient)
        {
            Segment(builder, "N3", Upper(patient.Address.Line1) ?? "UNKNOWN");
            Segment(builder, "N4", Upper(patient.Address.City) ?? "UNKNOWN", "", PostCode(patient.Address.PostCode));
            Segment(builder, "DMG", "D8", patient.DateOfBirth?.ToString("yyyyMMdd") ?? "", GenderCode(patient.Gender));
        }
        else
        {
            Segment(builder, "DMG", "D8",
                insurance.SubscriberDateOfBirth?.ToString("yyyyMMdd") ?? "", "U");
        }

        Segment(builder, "NM1", "PR", "2", carrier.Name.ToUpperInvariant(), "", "", "", "", "PI", payerId);

        // ---- loop 2000C patient, only when they are not the subscriber -------
        if (!subscriberIsPatient)
        {
            Segment(builder, "HL", "3", "2", "23", "0");
            Segment(builder, "PAT", RelationshipCode(insurance.RelationshipToSubscriber));
            Segment(builder, "NM1", "QC", "1", Upper(patient.Name.LastName), Upper(patient.Name.FirstName));
            Segment(builder, "N3", Upper(patient.Address.Line1) ?? "UNKNOWN");
            Segment(builder, "N4", Upper(patient.Address.City) ?? "UNKNOWN", "", PostCode(patient.Address.PostCode));
            Segment(builder, "DMG", "D8", patient.DateOfBirth?.ToString("yyyyMMdd") ?? "", GenderCode(patient.Gender));
        }

        // ---- loop 2300 claim --------------------------------------------------
        Segment(builder, "CLM",
            claim.ClaimNumber,
            Amount(claim.TotalCharged),
            "", "",
            Composite("11", "B", "1"),
            "Y", "A", "Y", "Y");

        Segment(builder, "DTP", "472", "D8", claim.ServiceDate.ToString("yyyyMMdd"));

        if (!string.IsNullOrWhiteSpace(claim.Narrative))
            Segment(builder, "NTE", "ADD", Upper(claim.Narrative)![..Math.Min(80, claim.Narrative.Length)]);

        // ---- loop 2400 service lines -------------------------------------------
        var lineNumber = 1;
        foreach (var line in claim.Lines.OrderBy(l => l.Sequence))
        {
            Segment(builder, "LX", lineNumber.ToString());

            var oralCavity = ToothDesignation(line);
            Segment(builder, "SV3",
                Composite("AD", line.ProcedureCode?.Code ?? "D9999"),
                Amount(line.ChargedAmount),
                "",
                oralCavity,
                "",
                "1");

            if (line.ToothId is not null && line.Tooth is not null)
            {
                Segment(builder, "TOO", "JP", line.Tooth.FdiNumber.ToString(),
                    string.IsNullOrWhiteSpace(line.SurfaceCode)
                        ? El.Raw(string.Empty)
                        : Composite(line.SurfaceCode.Select(surface => surface.ToString()).ToArray()));
            }

            Segment(builder, "DTP", "472", "D8", line.ServiceDate.ToString("yyyyMMdd"));
            lineNumber++;
        }

        // ---- trailers -----------------------------------------------------------
        // The segment count in SE includes ST and SE themselves.
        var segmentCount = builder.ToString().Split('~', StringSplitOptions.RemoveEmptyEntries).Length;
        var stIndex = IndexOfSegment(builder.ToString(), "ST");
        var transactionSegments = segmentCount - stIndex + 1;

        Segment(builder, "SE", transactionSegments.ToString(), transactionControl);
        Segment(builder, "GE", "1", groupControl);
        Segment(builder, "IEA", "1", controlNumber);

        return builder.ToString();
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    /// One element of a segment. Free text reaches this file straight out of the
    /// patient record, so data elements are scrubbed of the delimiters that would
    /// otherwise corrupt the interchange. Structural elements — composites and the
    /// fixed-width envelope fields — are written through verbatim, because there
    /// the delimiters and the padding are themselves the content.
    /// </summary>
    private readonly record struct El(string Value)
    {
        public static implicit operator El(string? data) => new(Sanitise(data));
        public static El Raw(string value) => new(value);
    }

    private static void Segment(StringBuilder builder, string tag, params El[] elements)
    {
        builder.Append(tag);

        // Trailing empty elements are omitted, as the standard expects.
        var last = elements.Length - 1;
        while (last >= 0 && string.IsNullOrEmpty(elements[last].Value)) last--;

        for (var i = 0; i <= last; i++)
        {
            builder.Append(ElementSeparator);
            builder.Append(elements[i].Value);
        }

        builder.Append(SegmentTerminator);
    }

    /// <summary>Delimiters inside data would corrupt the interchange.</summary>
    private static string Sanitise(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace(ElementSeparator, ' ')
                    .Replace(CompositeSeparator, ' ')
                    .Replace('~', ' ')
                    .Replace('\n', ' ')
                    .Replace('\r', ' ')
                    .Trim();
    }

    /// <summary>Joins sub-elements with the component separator, scrubbing each part first.</summary>
    private static El Composite(params string?[] parts) =>
        El.Raw(string.Join(CompositeSeparator, parts.Select(Sanitise)));

    private static int IndexOfSegment(string interchange, string tag)
    {
        var segments = interchange.Split('~', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
            if (segments[i].TrimStart().StartsWith(tag + ElementSeparator, StringComparison.Ordinal))
                return i + 1;
        return 1;
    }

    private static string ControlNumber(InsuranceClaim claim)
    {
        // Nine digits, stable for a given claim so a resubmission is recognisable.
        var digits = new string(claim.ClaimNumber.Where(char.IsDigit).ToArray());
        var value = digits.Length == 0 ? Math.Abs(claim.Id.GetHashCode()) : int.Parse(digits[^Math.Min(8, digits.Length)..]);
        return value.ToString("D9");
    }

    /// <summary>ISA elements are fixed width, so the padding has to survive to the wire.</summary>
    private static El Pad(string value, int length)
    {
        var clean = Sanitise(value);
        return El.Raw(clean.Length >= length ? clean[..length] : clean.PadRight(length));
    }

    private static string Amount(decimal value) => value.ToString("0.00");

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Upper(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    /// <summary>X12 postal codes carry no spaces.</summary>
    private static string PostCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "00000" : value.Replace(" ", "").ToUpperInvariant();

    private static string Digits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "0000000000" : new string(value.Where(char.IsDigit).ToArray());

    private static string GenderCode(Gender gender) => gender switch
    {
        Gender.Male => "M",
        Gender.Female => "F",
        _ => "U"
    };

    private static string PriorityCode(InsurancePriority priority) => priority switch
    {
        InsurancePriority.Primary => "P",
        InsurancePriority.Secondary => "S",
        _ => "T"
    };

    private static string RelationshipCode(ContactRelationship relationship) => relationship switch
    {
        ContactRelationship.Spouse or ContactRelationship.Partner => "01",
        ContactRelationship.Child => "19",
        ContactRelationship.Parent => "20",
        _ => "G8"
    };

    /// <summary>Maps the tooth to an oral cavity designation used by SV3.</summary>
    private static string ToothDesignation(InsuranceClaimLine line)
    {
        if (line.Tooth is null) return string.Empty;

        return line.Tooth.Quadrant switch
        {
            Quadrant.UpperRight or Quadrant.UpperRightPrimary => "UR",
            Quadrant.UpperLeft or Quadrant.UpperLeftPrimary => "UL",
            Quadrant.LowerLeft or Quadrant.LowerLeftPrimary => "LL",
            Quadrant.LowerRight or Quadrant.LowerRightPrimary => "LR",
            _ => string.Empty
        };
    }
}
