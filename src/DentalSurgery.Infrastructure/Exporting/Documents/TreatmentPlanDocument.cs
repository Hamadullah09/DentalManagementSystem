using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalSurgery.Infrastructure.Exporting.Documents;

/// <summary>
/// The written treatment plan given to the patient: what is proposed, what it
/// costs, what the risks and alternatives are, and a signature block.
/// </summary>
public class TreatmentPlanDocument(TreatmentPlan plan, Practice? practice) : IDocument
{
    private string Symbol => practice?.CurrencySymbol ?? "£";

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(PdfTheme.Body);

            page.Header().Element(header => PdfTheme.Letterhead(
                header, practice, "Treatment plan", $"{plan.PlanNumber} · version {plan.VersionNumber}"));

            page.Footer().Element(footer => PdfTheme.Footer(footer, practice,
                "This is an estimate. The final cost may change if treatment needs alter."));

            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Item().Element(Header);
                column.Item().PaddingTop(12).Element(Phases);
                column.Item().PaddingTop(10).Element(Summary);
                column.Item().PaddingTop(12).Element(RisksAndAlternatives);
                column.Item().PaddingTop(14).Element(Consent);
            });
        });
    }

    private void Header(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                left.Item().Text("PREPARED FOR").Style(PdfTheme.Label);
                left.Item().Text(plan.Patient?.Name.Full ?? "Patient").Style(PdfTheme.Body).Bold();
                if (plan.Patient is not null)
                {
                    left.Item().Text($"Patient number {plan.Patient.PatientNumber}").Style(PdfTheme.Small);
                    if (plan.Patient.DateOfBirth is { } dob)
                        left.Item().Text($"Date of birth {dob:d MMMM yyyy}").Style(PdfTheme.Small);
                }
            });

            row.ConstantItem(210).Column(right =>
            {
                PdfTheme.Field(right, "Plan", plan.Name);
                PdfTheme.Field(right, "Clinician", plan.Provider?.DisplayName);
                PdfTheme.Field(right, "Prepared", plan.CreatedOn.ToString("d MMMM yyyy"));
                PdfTheme.Field(right, "Valid until", plan.ValidUntil?.ToString("d MMMM yyyy"));
                PdfTheme.Field(right, "Status", PdfTheme.Humanise(plan.Status));
            });
        });
    }

    private void Phases(IContainer container)
    {
        container.Column(column =>
        {
            foreach (var phase in plan.Phases.OrderBy(p => p.PhaseNumber))
            {
                if (phase.Items.Count == 0) continue;

                column.Item().PaddingTop(8).Background(PdfTheme.Surface).Padding(6).Row(row =>
                {
                    row.RelativeItem().Column(heading =>
                    {
                        heading.Item().Text(phase.Name).Style(PdfTheme.Body).Bold();
                        if (!string.IsNullOrWhiteSpace(phase.ClinicalObjective))
                            heading.Item().Text(phase.ClinicalObjective).Style(PdfTheme.Small);
                    });

                    row.ConstantItem(90).AlignRight().Text(PdfTheme.Money(phase.PhaseTotal, Symbol))
                        .Style(PdfTheme.Body).Bold();
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(44);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(36);
                        columns.ConstantColumn(46);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(64);
                        columns.ConstantColumn(60);
                    });

                    table.Header(header =>
                    {
                        AddHeader(header, "Code", false);
                        AddHeader(header, "Treatment", false);
                        AddHeader(header, "Tooth", false);
                        AddHeader(header, "Surfaces", false);
                        AddHeader(header, "Fee", true);
                        AddHeader(header, "Insurance", true);
                        AddHeader(header, "You pay", true);
                    });

                    foreach (var item in phase.Items.OrderBy(i => i.Sequence))
                    {
                        var declined = item.Status == TreatmentPlanItemStatus.Declined;
                        var colour = declined ? PdfTheme.Faint : PdfTheme.Ink;

                        Cell(table).Text(item.ProcedureCode?.Code ?? "-")
                            .Style(PdfTheme.Small).FontColor(colour);

                        Cell(table).Column(description =>
                        {
                            description.Item().Text(item.ProcedureCode?.ShortDescription ?? "Procedure")
                                .Style(PdfTheme.Small).FontColor(colour);
                            if (declined)
                                description.Item().Text("Declined").Style(PdfTheme.Small)
                                    .FontSize(6.5f).FontColor(PdfTheme.Danger);
                            else if (item.Status == TreatmentPlanItemStatus.Completed)
                                description.Item().Text("Completed").Style(PdfTheme.Small)
                                    .FontSize(6.5f).FontColor(PdfTheme.Success);
                        });

                        Cell(table).Text(item.Tooth?.FdiNumber.ToString() ?? "-")
                            .Style(PdfTheme.Small).FontColor(colour);

                        Cell(table).Text(item.Surfaces == ToothSurface.None
                                ? "-" : SurfaceNotation.ToCode(item.Surfaces))
                            .Style(PdfTheme.Small).FontColor(colour);

                        Cell(table).AlignRight().Text(PdfTheme.Money(item.NetFee, Symbol))
                            .Style(PdfTheme.Small).FontColor(colour);

                        Cell(table).AlignRight().Text(item.EstimatedInsurance > 0
                                ? PdfTheme.Money(item.EstimatedInsurance, Symbol) : "-")
                            .Style(PdfTheme.Small).FontColor(colour);

                        Cell(table).AlignRight().Text(PdfTheme.Money(item.EstimatedPatient, Symbol))
                            .Style(PdfTheme.Small).Bold().FontColor(colour);
                    }
                });
            }
        });
    }

    private static void AddHeader(TableCellDescriptor header, string text, bool right)
    {
        var cell = header.Cell().BorderBottom(1).BorderColor(PdfTheme.LineStrong)
            .PaddingVertical(3).PaddingHorizontal(3);

        (right ? cell.AlignRight() : cell.AlignLeft()).Text(text).Style(PdfTheme.TableHeader);
    }

    private static IContainer Cell(TableDescriptor table) =>
        table.Cell().BorderBottom(0.5f).BorderColor(PdfTheme.Line).PaddingVertical(3).PaddingHorizontal(3);

    private void Summary(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem();

            row.ConstantItem(250).Element(panel => PdfTheme.Panel(panel, PdfTheme.AccentSoft).Column(column =>
            {
                Total(column, "Total treatment cost", PdfTheme.Money(plan.TotalFee, Symbol));

                if (plan.TotalDiscount > 0)
                    Total(column, "Discount", "-" + PdfTheme.Money(plan.TotalDiscount, Symbol));

                if (plan.EstimatedInsurancePortion > 0)
                    Total(column, "Estimated insurance", "-" + PdfTheme.Money(plan.EstimatedInsurancePortion, Symbol));

                column.Item().PaddingVertical(3).LineHorizontal(1).LineColor(PdfTheme.LineStrong);

                column.Item().Row(final =>
                {
                    final.RelativeItem().Text("Estimated cost to you").Style(PdfTheme.Body).Bold();
                    final.ConstantItem(90).AlignRight()
                        .Text(PdfTheme.Money(plan.EstimatedPatientPortion, Symbol))
                        .Style(PdfTheme.Body).FontSize(13).Bold().FontColor(PdfTheme.Accent);
                });
            }));
        });
    }

    private static void Total(ColumnDescriptor column, string label, string value)
    {
        column.Item().PaddingVertical(1).Row(row =>
        {
            row.RelativeItem().Text(label).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
            row.ConstantItem(90).AlignRight().Text(value).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
        });
    }

    private void RisksAndAlternatives(IContainer container)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(plan.RisksDiscussed))
            {
                column.Item().Text("Risks discussed").Style(PdfTheme.Label);
                column.Item().PaddingBottom(6).Text(plan.RisksDiscussed).Style(PdfTheme.Small);
            }

            if (!string.IsNullOrWhiteSpace(plan.AlternativesDiscussed))
            {
                column.Item().Text("Alternatives discussed").Style(PdfTheme.Label);
                column.Item().PaddingBottom(6).Text(plan.AlternativesDiscussed).Style(PdfTheme.Small);
            }

            if (!string.IsNullOrWhiteSpace(plan.Notes))
            {
                column.Item().Text("Notes").Style(PdfTheme.Label);
                column.Item().Text(plan.Notes).Style(PdfTheme.Small);
            }
        });
    }

    private void Consent(IContainer container)
    {
        container.Element(panel => PdfTheme.Panel(panel).Column(column =>
        {
            column.Item().Text("Agreement").Style(PdfTheme.Label);
            column.Item().PaddingBottom(10).Text(
                "I have read this plan, I have had the chance to ask questions, and I understand that " +
                "the figures are an estimate that may change if my treatment needs alter. I understand " +
                "I may decline any item.").Style(PdfTheme.Small);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(signature =>
                {
                    signature.Item().PaddingTop(18).LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                    signature.Item().Text("Patient signature").Style(PdfTheme.Small);
                });

                row.ConstantItem(20);

                row.ConstantItem(130).Column(date =>
                {
                    date.Item().PaddingTop(18).LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                    date.Item().Text("Date").Style(PdfTheme.Small);
                });
            });

            if (plan.ConsentObtained && plan.ConsentSignedAtUtc is { } signed)
            {
                column.Item().PaddingTop(6)
                    .Text($"Consent recorded electronically on {signed.ToLocalTime():d MMMM yyyy HH:mm}.")
                    .Style(PdfTheme.Small).FontColor(PdfTheme.Success);
            }
        }));
    }
}
