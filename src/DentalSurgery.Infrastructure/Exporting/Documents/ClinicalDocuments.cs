using DentalSurgery.Application.Clinical;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalSurgery.Infrastructure.Exporting.Documents;

/// <summary>Everything a clinical summary or referral pack needs.</summary>
public class ClinicalSummaryData
{
    public required Patient Patient { get; init; }
    public MedicalRiskProfile Risk { get; init; } = new();
    public IReadOnlyList<PatientAllergy> Allergies { get; init; } = Array.Empty<PatientAllergy>();
    public IReadOnlyList<PatientMedicalCondition> Conditions { get; init; } = Array.Empty<PatientMedicalCondition>();
    public IReadOnlyList<PatientMedication> Medications { get; init; } = Array.Empty<PatientMedication>();
    public IReadOnlyList<Procedure> Procedures { get; init; } = Array.Empty<Procedure>();
    public IReadOnlyList<ClinicalNote> Notes { get; init; } = Array.Empty<ClinicalNote>();
    public IReadOnlyList<RadiographRecord> Radiographs { get; init; } = Array.Empty<RadiographRecord>();
    public DentalChart? Chart { get; init; }
    public PeriodontalChart? Perio { get; init; }
    public PeriodontalSummary? PerioSummary { get; init; }
    public Referral? Referral { get; init; }
}

/// <summary>
/// A clinical summary of the patient, suitable for handing to the patient, a
/// specialist or a new practice. Doubles as the body of a referral letter.
/// </summary>
public class ClinicalSummaryDocument(ClinicalSummaryData data, Practice? practice) : IDocument
{
    private Patient Patient => data.Patient;
    private bool IsReferral => data.Referral is not null;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(PdfTheme.Body);

            page.Header().Element(header => PdfTheme.Letterhead(
                header, practice,
                IsReferral ? "Referral letter" : "Clinical summary",
                IsReferral ? data.Referral!.ReferralNumber : Patient.PatientNumber));

            page.Footer().Element(footer => PdfTheme.Footer(footer, practice,
                "Confidential patient information. Handle under the practice data protection policy."));

            page.Content().PaddingVertical(14).Column(column =>
            {
                if (IsReferral) column.Item().PaddingBottom(10).Element(ReferralHeader);

                column.Item().Element(PatientBlock);

                if (data.Risk.HasAnyFlags)
                    column.Item().PaddingTop(10).Element(RiskBlock);

                column.Item().PaddingTop(10).Element(MedicalBlock);

                if (data.Chart is not null)
                    column.Item().PaddingTop(10).Element(ChartBlock);

                if (data.PerioSummary is not null)
                    column.Item().PaddingTop(10).Element(PerioBlock);

                if (data.Procedures.Count > 0)
                    column.Item().PaddingTop(10).Element(TreatmentBlock);

                if (data.Radiographs.Count > 0)
                    column.Item().PaddingTop(10).Element(RadiographBlock);

                if (data.Notes.Count > 0)
                    column.Item().PaddingTop(10).Element(NotesBlock);

                if (IsReferral) column.Item().PaddingTop(14).Element(SignOff);
            });
        });
    }

    private void ReferralHeader(IContainer container)
    {
        var referral = data.Referral!;

        container.Element(panel => PdfTheme.Panel(panel).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("REFERRED TO").Style(PdfTheme.Label);
                    left.Item().Text(referral.ExternalProviderName ?? "Specialist").Style(PdfTheme.Body).Bold();
                    if (!string.IsNullOrWhiteSpace(referral.ExternalPracticeName))
                        left.Item().Text(referral.ExternalPracticeName).Style(PdfTheme.Small);
                    if (!string.IsNullOrWhiteSpace(referral.ExternalSpecialty))
                        left.Item().Text(referral.ExternalSpecialty).Style(PdfTheme.Small);
                    if (referral.ExternalAddress is not null)
                        foreach (var line in referral.ExternalAddress.ToLines())
                            left.Item().Text(line).Style(PdfTheme.Small);
                });

                row.ConstantItem(190).Column(right =>
                {
                    PdfTheme.Field(right, "Date", referral.ReferralDate.ToString("d MMMM yyyy"));
                    PdfTheme.Field(right, "Urgency", PdfTheme.Humanise(referral.Priority));
                    PdfTheme.Field(right, "Referred by", referral.InternalProvider?.DisplayName);
                });
            });

            column.Item().PaddingTop(8).Text("Reason for referral").Style(PdfTheme.Label);
            column.Item().Text(referral.Reason).Style(PdfTheme.Body);

            if (!string.IsNullOrWhiteSpace(referral.ClinicalSummary))
            {
                column.Item().PaddingTop(6).Text("Clinical summary").Style(PdfTheme.Label);
                column.Item().Text(referral.ClinicalSummary).Style(PdfTheme.Small);
            }
        }));
    }

    private void PatientBlock(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                left.Item().Text("PATIENT").Style(PdfTheme.Label);
                left.Item().Text(Patient.Name.Full).Style(PdfTheme.Body).FontSize(12).Bold();
                foreach (var line in Patient.Address.ToLines())
                    left.Item().Text(line).Style(PdfTheme.Small);

                var contact = Patient.Contact.BestPhone;
                if (!string.IsNullOrWhiteSpace(contact))
                    left.Item().PaddingTop(2).Text(contact).Style(PdfTheme.Small);
            });

            row.ConstantItem(220).Column(right =>
            {
                PdfTheme.Field(right, "Patient number", Patient.PatientNumber);
                PdfTheme.Field(right, "Date of birth",
                    Patient.DateOfBirth is { } dob ? $"{dob:d MMMM yyyy} ({Patient.AgeYears})" : null);
                PdfTheme.Field(right, "Gender", PdfTheme.Humanise(Patient.Gender));
                PdfTheme.Field(right, "NHS number", Patient.NhsNumber);
                PdfTheme.Field(right, "Clinician", Patient.PrimaryProvider?.DisplayName);
                PdfTheme.Field(right, "GP", Patient.GeneralPractitionerName);
            });
        });
    }

    private void RiskBlock(IContainer container)
    {
        container.Element(panel => PdfTheme.Panel(panel, "#FDEAEA").Column(column =>
        {
            column.Item().Text("CLINICAL ALERTS").Style(PdfTheme.Label).FontColor(PdfTheme.Danger);

            foreach (var flag in data.Risk.Flags.Take(12))
            {
                column.Item().PaddingTop(2).Text(text =>
                {
                    text.DefaultTextStyle(PdfTheme.Small.FontColor(PdfTheme.Ink));
                    text.Span($"{flag.Title}. ").Bold();
                    text.Span(flag.Detail);
                });
            }

            column.Item().PaddingTop(4).Text(
                $"ASA {PdfTheme.Humanise(data.Risk.EstimatedAsa).Replace("Asa ", "")}" +
                (data.Risk.RequiresAntibioticProphylaxis ? " · antibiotic prophylaxis indicated" : "") +
                (data.Risk.BleedingRisk ? " · bleeding risk" : "") +
                (data.Risk.MronjRisk ? " · MRONJ risk" : "") +
                (data.Risk.LatexAllergy ? " · latex free" : ""))
                .Style(PdfTheme.Small).Bold();
        }));
    }

    private void MedicalBlock(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().PaddingRight(8).Column(column =>
            {
                column.Item().Text("MEDICAL HISTORY").Style(PdfTheme.Label);

                if (data.Conditions.Count == 0)
                    column.Item().Text("No conditions recorded.").Style(PdfTheme.Small);

                foreach (var condition in data.Conditions.Take(14))
                    column.Item().Text($"• {condition.DisplayName} ({PdfTheme.Humanise(condition.Status)})")
                        .Style(PdfTheme.Small);

                column.Item().PaddingTop(6).Text("ALLERGIES").Style(PdfTheme.Label);
                if (data.Allergies.Count == 0)
                    column.Item().Text("None known.").Style(PdfTheme.Small);

                foreach (var allergy in data.Allergies)
                    column.Item().Text($"• {allergy.DisplayName} — {PdfTheme.Humanise(allergy.Severity)}" +
                                       (string.IsNullOrWhiteSpace(allergy.Reaction) ? "" : $" ({allergy.Reaction})"))
                        .Style(PdfTheme.Small)
                        .FontColor(allergy.IsCritical ? PdfTheme.Danger : PdfTheme.Ink);
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().Text("CURRENT MEDICATION").Style(PdfTheme.Label);

                if (data.Medications.Count == 0)
                    column.Item().Text("None recorded.").Style(PdfTheme.Small);

                foreach (var medication in data.Medications.Take(16))
                    column.Item().Text($"• {medication.DisplayName}" +
                                       (string.IsNullOrWhiteSpace(medication.Dosage) ? "" : $" {medication.Dosage}") +
                                       (string.IsNullOrWhiteSpace(medication.Frequency) ? "" : $", {medication.Frequency}"))
                        .Style(PdfTheme.Small);
            });
        });
    }

    private void ChartBlock(IContainer container)
    {
        var chart = data.Chart!;

        container.Column(column =>
        {
            column.Item().Text("DENTAL CHART").Style(PdfTheme.Label);

            column.Item().Text(
                $"DMFT {chart.Dmft} · {chart.PresentCount} teeth present · {chart.MissingCount} missing · " +
                $"{chart.RestoredCount} restored · {chart.CariousCount} carious · " +
                $"{chart.CrownedCount} crowned · {chart.ImplantCount} implants · " +
                $"{chart.RootTreatedCount} root treated")
                .Style(PdfTheme.Small);

            var notable = chart.AllTeeth
                .Where(t => t.IsMissing || t.HasCaries || t.IsImplant || t.IsCrowned || t.IsRootTreated || t.HasPlannedWork)
                .OrderBy(t => t.Tooth.ChartOrder)
                .ToList();

            if (notable.Count == 0)
            {
                column.Item().Text("All teeth charted as sound.").Style(PdfTheme.Small);
                return;
            }

            column.Item().PaddingTop(4).Text(string.Join("   ",
                notable.Select(t => $"{t.Tooth.FdiNumber}: {t.StatusSummary}")))
                .Style(PdfTheme.Small);
        });
    }

    private void PerioBlock(IContainer container)
    {
        var summary = data.PerioSummary!;
        var chart = data.Perio;

        container.Column(column =>
        {
            column.Item().Text("PERIODONTAL").Style(PdfTheme.Label);

            column.Item().Text(
                $"{PdfTheme.Humanise(summary.SuggestedDiagnosis)}" +
                (chart is null ? "" : $" · examined {chart.ExamDate:d MMM yyyy}"))
                .Style(PdfTheme.Small).Bold();

            column.Item().Text(
                $"Mean pocket depth {summary.MeanPocketDepth:0.0} mm, deepest {summary.MaxPocketDepth} mm. " +
                $"Bleeding on probing {summary.BleedingPercent:0.#}%, plaque {summary.PlaquePercent:0.#}%. " +
                $"{summary.Sites4To5mm} sites 4-5 mm, {summary.Sites6To7mm + summary.Sites8mmPlus} sites 6 mm or more. " +
                $"Furcation involvement on {summary.FurcationInvolvedTeeth} teeth.")
                .Style(PdfTheme.Small);

            if (chart is not null)
            {
                column.Item().PaddingTop(2).Text(
                    $"BPE  {chart.BpeUpperRight ?? "-"} | {chart.BpeUpperAnterior ?? "-"} | {chart.BpeUpperLeft ?? "-"}   " +
                    $"{chart.BpeLowerRight ?? "-"} | {chart.BpeLowerAnterior ?? "-"} | {chart.BpeLowerLeft ?? "-"}")
                    .Style(PdfTheme.Small);
            }
        });
    }

    private void TreatmentBlock(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(3).Text("RECENT TREATMENT").Style(PdfTheme.Label);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(58);
                    columns.ConstantColumn(42);
                    columns.RelativeColumn();
                    columns.ConstantColumn(34);
                    columns.ConstantColumn(110);
                });

                foreach (var procedure in data.Procedures.Take(25))
                {
                    Cell(table).Text(procedure.DateOfService.ToString("dd/MM/yy")).Style(PdfTheme.Small);
                    Cell(table).Text(procedure.ProcedureCode?.Code ?? "-").Style(PdfTheme.Small);
                    Cell(table).Text(procedure.ProcedureCode?.ShortDescription ?? "-").Style(PdfTheme.Small);
                    Cell(table).Text(procedure.Tooth?.FdiNumber.ToString() ?? "-").Style(PdfTheme.Small);
                    Cell(table).Text(procedure.Provider?.DisplayName ?? "-").Style(PdfTheme.Small);
                }
            });
        });
    }

    private void RadiographBlock(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("IMAGING").Style(PdfTheme.Label);

            foreach (var radiograph in data.Radiographs.Take(10))
            {
                column.Item().Text(text =>
                {
                    text.DefaultTextStyle(PdfTheme.Small);
                    text.Span($"{radiograph.TakenAtUtc:dd/MM/yy} ").Bold();
                    text.Span(PdfTheme.Humanise(radiograph.RadiographType));
                    if (!string.IsNullOrWhiteSpace(radiograph.ToothNumbers))
                        text.Span($" ({radiograph.ToothNumbers})");
                    if (!string.IsNullOrWhiteSpace(radiograph.Findings))
                        text.Span($" — {radiograph.Findings}");
                });
            }
        });
    }

    private void NotesBlock(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(3).Text("CLINICAL NOTES").Style(PdfTheme.Label);

            foreach (var note in data.Notes.Take(6))
            {
                column.Item().PaddingBottom(6).Column(entry =>
                {
                    entry.Item().Text(text =>
                    {
                        text.DefaultTextStyle(PdfTheme.Small);
                        text.Span($"{note.NoteDateUtc:d MMM yyyy} · ").Bold();
                        text.Span(PdfTheme.Humanise(note.NoteType));
                        text.Span($" · {note.Provider?.DisplayName ?? "unknown clinician"}");
                        if (note.IsSigned) text.Span("  (signed)");
                    });

                    foreach (var (label, value) in new[]
                    {
                        ("Complaint", note.ChiefComplaint),
                        ("Subjective", note.Subjective),
                        ("Objective", note.Objective),
                        ("Assessment", note.Assessment),
                        ("Plan", note.Plan),
                        ("Note", note.Body)
                    })
                    {
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        entry.Item().Text(text =>
                        {
                            text.DefaultTextStyle(PdfTheme.Small.FontSize(7));
                            text.Span($"{label}: ").Bold();
                            text.Span(value);
                        });
                    }
                });
            }
        });
    }

    private void SignOff(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Thank you for seeing this patient. Please contact the practice if you need " +
                               "any further information or copies of the radiographs.").Style(PdfTheme.Small);

            column.Item().PaddingTop(20).Row(row =>
            {
                row.ConstantItem(200).Column(signature =>
                {
                    signature.Item().LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                    signature.Item().Text(data.Referral?.InternalProvider?.DisplayName ?? "Referring clinician")
                        .Style(PdfTheme.Small).Bold();
                    if (!string.IsNullOrWhiteSpace(data.Referral?.InternalProvider?.RegistrationNumber))
                        signature.Item().Text($"Registration {data.Referral.InternalProvider.RegistrationNumber}")
                            .Style(PdfTheme.Small);
                });
            });
        });
    }

    private static IContainer Cell(TableDescriptor table) =>
        table.Cell().BorderBottom(0.5f).BorderColor(PdfTheme.Line).PaddingVertical(2).PaddingHorizontal(2);
}

/// <summary>A printable prescription.</summary>
public class PrescriptionDocument(Prescription prescription, Practice? practice) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(PdfTheme.Body);

            page.Header().Element(header => PdfTheme.Letterhead(
                header, practice, "Prescription", prescription.PrescriptionNumber));

            page.Footer().Element(footer => PdfTheme.Footer(footer, practice,
                "Keep this prescription safe. It cannot be replaced if lost."));

            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("PATIENT").Style(PdfTheme.Label);
                        left.Item().Text(prescription.Patient?.Name.Full ?? "Patient").Style(PdfTheme.Body).Bold();
                        if (prescription.Patient?.DateOfBirth is { } dob)
                            left.Item().Text($"Date of birth {dob:d MMMM yyyy}").Style(PdfTheme.Small);
                        foreach (var line in prescription.Patient?.Address.ToLines() ?? Array.Empty<string>())
                            left.Item().Text(line).Style(PdfTheme.Small);
                    });

                    row.ConstantItem(180).Column(right =>
                    {
                        PdfTheme.Field(right, "Issued", prescription.IssueDate.ToString("d MMMM yyyy"));
                        PdfTheme.Field(right, "Valid until", prescription.ValidUntil?.ToString("d MMMM yyyy"));
                        PdfTheme.Field(right, "Prescriber", prescription.PrescriberStaff?.DisplayName);
                        PdfTheme.Field(right, "Registration", prescription.PrescriberStaff?.RegistrationNumber);
                    });
                });

                column.Item().PaddingTop(10).Text("Rx").Style(PdfTheme.Heading).FontColor(PdfTheme.Accent);

                foreach (var item in prescription.Items.OrderBy(i => i.Sequence))
                {
                    column.Item().PaddingTop(6).Element(panel => PdfTheme.Panel(panel).Column(entry =>
                    {
                        entry.Item().Text(item.DisplayName).Style(PdfTheme.Body).Bold();
                        entry.Item().Text($"{item.Sig} · {PdfTheme.Humanise(item.Route)}").Style(PdfTheme.Small);
                        entry.Item().Text($"Supply: {item.Quantity:0.##} {item.Unit}" +
                                          (item.Repeats > 0 ? $" · {item.Repeats} repeat(s)" : ""))
                            .Style(PdfTheme.Small);

                        if (!string.IsNullOrWhiteSpace(item.Instructions))
                            entry.Item().Text(item.Instructions).Style(PdfTheme.Small).Italic();

                        if (item.Medication?.IsControlledDrug == true)
                            entry.Item().PaddingTop(2)
                                .Text($"CONTROLLED DRUG — schedule {item.Medication.ControlledSchedule}. " +
                                      $"Quantity in words: {InWords(item.Quantity)}.")
                                .Style(PdfTheme.Small).Bold().FontColor(PdfTheme.Danger);
                    }));
                }

                if (!string.IsNullOrWhiteSpace(prescription.Indication))
                    column.Item().PaddingTop(8).Text($"Indication: {prescription.Indication}").Style(PdfTheme.Small);

                column.Item().PaddingTop(20).Row(row =>
                {
                    row.ConstantItem(220).Column(signature =>
                    {
                        signature.Item().LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                        signature.Item().Text("Prescriber signature").Style(PdfTheme.Small);
                    });
                });
            });
        });
    }

    /// <summary>Controlled drug quantities must be written in words as well as figures.</summary>
    private static string InWords(decimal quantity)
    {
        var whole = (int)Math.Round(quantity);
        string[] units =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
            "eighteen", "nineteen", "twenty"
        };

        if (whole <= 20) return units[whole];

        string[] tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        if (whole < 100)
            return whole % 10 == 0 ? tens[whole / 10] : $"{tens[whole / 10]}-{units[whole % 10]}";

        return whole.ToString();
    }
}
