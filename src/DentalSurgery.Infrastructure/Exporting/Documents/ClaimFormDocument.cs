using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalSurgery.Infrastructure.Exporting.Documents;

/// <summary>
/// A dental claim form laid out along the lines of the ADA J430, for payers who
/// accept paper or a portal upload rather than an electronic interchange.
/// </summary>
public class ClaimFormDocument(InsuranceClaim claim, Practice? practice) : IDocument
{
    private string Symbol => practice?.CurrencySymbol ?? "£";

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(PdfTheme.Body);

            page.Header().Element(header => PdfTheme.Letterhead(
                header, practice,
                claim.IsPreAuthorisation ? "Pre-authorisation request" : "Dental claim form",
                claim.ClaimNumber));

            page.Footer().Element(footer => PdfTheme.Footer(footer, practice,
                "I certify that the procedures listed have been completed as indicated."));

            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Item().Element(PayerAndPolicy);
                column.Item().PaddingTop(10).Element(PatientAndSubscriber);
                column.Item().PaddingTop(10).Element(ServiceLines);
                column.Item().PaddingTop(10).Element(Totals);
                column.Item().PaddingTop(12).Element(Certification);
            });
        });
    }

    private void PayerAndPolicy(IContainer container)
    {
        var plan = claim.PatientInsurance?.InsurancePlan;
        var carrier = plan?.InsuranceCarrier;

        container.Row(row =>
        {
            row.RelativeItem().PaddingRight(10).Element(panel => PdfTheme.Panel(panel).Column(column =>
            {
                column.Item().Text("PAYER").Style(PdfTheme.Label);
                column.Item().Text(carrier?.Name ?? "Insurance carrier").Style(PdfTheme.Body).Bold();

                foreach (var line in (carrier?.ClaimsAddress ?? carrier?.Address)?.ToLines() ?? Array.Empty<string>())
                    column.Item().Text(line).Style(PdfTheme.Small);

                PdfTheme.Field(column, "Payer ID", carrier?.ElectronicPayerId ?? carrier?.PayerId);
                PdfTheme.Field(column, "Plan", plan?.PlanName);
                PdfTheme.Field(column, "Group", plan?.GroupNumber);
            }));

            row.RelativeItem().Element(panel => PdfTheme.Panel(panel).Column(column =>
            {
                column.Item().Text("CLAIM").Style(PdfTheme.Label);
                PdfTheme.Field(column, "Claim number", claim.ClaimNumber);
                PdfTheme.Field(column, "Type", claim.IsPreAuthorisation ? "Pre-authorisation" : "Statement of services");
                PdfTheme.Field(column, "Service date", claim.ServiceDate.ToString("d MMMM yyyy"));
                PdfTheme.Field(column, "Submitted", claim.SubmittedOn?.ToString("d MMMM yyyy") ?? "Not yet submitted");
                PdfTheme.Field(column, "Status", PdfTheme.Humanise(claim.Status));
                if (claim.ResubmissionCount > 0)
                    PdfTheme.Field(column, "Resubmission", claim.ResubmissionCount.ToString());
            }));
        });
    }

    private void PatientAndSubscriber(IContainer container)
    {
        var insurance = claim.PatientInsurance;
        var patient = claim.Patient;

        container.Row(row =>
        {
            row.RelativeItem().PaddingRight(10).Column(column =>
            {
                column.Item().Text("PATIENT").Style(PdfTheme.Label);
                column.Item().Text(patient?.Name.Full ?? "Patient").Style(PdfTheme.Body).Bold();
                PdfTheme.Field(column, "Date of birth", patient?.DateOfBirth?.ToString("dd/MM/yyyy"));
                PdfTheme.Field(column, "Gender", PdfTheme.Humanise(patient?.Gender));
                PdfTheme.Field(column, "Patient number", patient?.PatientNumber);
                foreach (var line in patient?.Address.ToLines() ?? Array.Empty<string>())
                    column.Item().Text(line).Style(PdfTheme.Small);
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().Text("SUBSCRIBER").Style(PdfTheme.Label);

                var subscriberName = insurance?.SubscriberIsPatient == true
                    ? patient?.Name.Full
                    : insurance?.SubscriberName?.Full;

                column.Item().Text(subscriberName ?? "Subscriber").Style(PdfTheme.Body).Bold();
                PdfTheme.Field(column, "Member ID", insurance?.MemberId ?? insurance?.PolicyNumber);
                PdfTheme.Field(column, "Relationship", insurance?.SubscriberIsPatient == true
                    ? "Self" : PdfTheme.Humanise(insurance?.RelationshipToSubscriber));
                PdfTheme.Field(column, "Cover", PdfTheme.Humanise(insurance?.Priority));
                PdfTheme.Field(column, "Effective from", insurance?.EffectiveFrom.ToString("dd/MM/yyyy"));
                PdfTheme.Field(column, "Employer", insurance?.SubscriberEmployer);
            });
        });
    }

    private void ServiceLines(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Text("RECORD OF SERVICES PROVIDED").Style(PdfTheme.Label);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(26);   // line
                    columns.ConstantColumn(58);   // date
                    columns.ConstantColumn(34);   // tooth
                    columns.ConstantColumn(46);   // surfaces
                    columns.ConstantColumn(46);   // code
                    columns.RelativeColumn();     // description
                    columns.ConstantColumn(58);   // charged
                    columns.ConstantColumn(58);   // allowed
                    columns.ConstantColumn(58);   // paid
                });

                table.Header(header =>
                {
                    foreach (var (text, right) in new (string, bool)[]
                    {
                        ("#", false), ("Date", false), ("Tooth", false), ("Surf", false),
                        ("Code", false), ("Description", false),
                        ("Charged", true), ("Allowed", true), ("Paid", true)
                    })
                    {
                        var cell = header.Cell().Background(PdfTheme.Surface)
                            .BorderBottom(1).BorderColor(PdfTheme.LineStrong)
                            .PaddingVertical(3).PaddingHorizontal(2);

                        (right ? cell.AlignRight() : cell.AlignLeft()).Text(text).Style(PdfTheme.TableHeader);
                    }
                });

                foreach (var line in claim.Lines.OrderBy(l => l.Sequence))
                {
                    Cell(table).Text(line.Sequence.ToString()).Style(PdfTheme.Small);
                    Cell(table).Text(line.ServiceDate.ToString("dd/MM/yy")).Style(PdfTheme.Small);
                    Cell(table).Text(line.Tooth?.FdiNumber.ToString() ?? "-").Style(PdfTheme.Small);
                    Cell(table).Text(string.IsNullOrWhiteSpace(line.SurfaceCode) ? "-" : line.SurfaceCode).Style(PdfTheme.Small);
                    Cell(table).Text(line.ProcedureCode?.Code ?? "-").Style(PdfTheme.Small);
                    Cell(table).Text(line.ProcedureCode?.ShortDescription ?? "-").Style(PdfTheme.Small);
                    Cell(table).AlignRight().Text(PdfTheme.Money(line.ChargedAmount, Symbol)).Style(PdfTheme.Small);
                    Cell(table).AlignRight().Text(line.AllowedAmount > 0 ? PdfTheme.Money(line.AllowedAmount, Symbol) : "-").Style(PdfTheme.Small);
                    Cell(table).AlignRight().Text(line.PaidAmount > 0 ? PdfTheme.Money(line.PaidAmount, Symbol) : "-").Style(PdfTheme.Small);
                }
            });
        });
    }

    private static IContainer Cell(TableDescriptor table) =>
        table.Cell().BorderBottom(0.5f).BorderColor(PdfTheme.Line).PaddingVertical(3).PaddingHorizontal(2);

    private void Totals(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(narrative =>
            {
                if (!string.IsNullOrWhiteSpace(claim.Narrative))
                {
                    narrative.Item().Text("REMARKS").Style(PdfTheme.Label);
                    narrative.Item().Text(claim.Narrative).Style(PdfTheme.Small);
                }

                if (!string.IsNullOrWhiteSpace(claim.DenialReason))
                {
                    narrative.Item().PaddingTop(4).Text("DENIAL REASON").Style(PdfTheme.Label).FontColor(PdfTheme.Danger);
                    narrative.Item().Text($"{claim.DenialCode} {claim.DenialReason}".Trim())
                        .Style(PdfTheme.Small).FontColor(PdfTheme.Danger);
                }
            });

            row.ConstantItem(230).Element(panel => PdfTheme.Panel(panel).Column(column =>
            {
                Line(column, "Total charged", PdfTheme.Money(claim.TotalCharged, Symbol));
                Line(column, "Total allowed", PdfTheme.Money(claim.TotalAllowed, Symbol));
                Line(column, "Deductible applied", PdfTheme.Money(claim.DeductibleApplied, Symbol));
                Line(column, "Paid by payer", PdfTheme.Money(claim.TotalPaid, Symbol));
                Line(column, "Contractual write-off", PdfTheme.Money(claim.WriteOffAmount, Symbol));

                column.Item().PaddingVertical(3).LineHorizontal(1).LineColor(PdfTheme.LineStrong);

                column.Item().Row(final =>
                {
                    final.RelativeItem().Text("Patient responsibility").Style(PdfTheme.Small).Bold();
                    final.ConstantItem(80).AlignRight()
                        .Text(PdfTheme.Money(claim.PatientResponsibility, Symbol))
                        .Style(PdfTheme.Body).Bold();
                });
            }));
        });
    }

    private static void Line(ColumnDescriptor column, string label, string value)
    {
        column.Item().PaddingVertical(1).Row(row =>
        {
            row.RelativeItem().Text(label).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
            row.ConstantItem(80).AlignRight().Text(value).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
        });
    }

    private void Certification(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(
                "I certify that the procedures listed above have been completed on the dates shown, that the " +
                "fees are those normally charged, and that the information given is accurate to the best of my " +
                "knowledge.").Style(PdfTheme.Small);

            column.Item().PaddingTop(18).Row(row =>
            {
                row.RelativeItem().PaddingRight(20).Column(signature =>
                {
                    signature.Item().LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                    signature.Item().Text(claim.Provider?.DisplayName ?? "Treating clinician")
                        .Style(PdfTheme.Small).Bold();
                    if (!string.IsNullOrWhiteSpace(claim.Provider?.RegistrationNumber))
                        signature.Item().Text($"Registration {claim.Provider.RegistrationNumber}").Style(PdfTheme.Small);
                });

                row.ConstantItem(130).Column(date =>
                {
                    date.Item().LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                    date.Item().Text("Date").Style(PdfTheme.Small);
                });
            });
        });
    }
}
