using DentalSurgery.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalSurgery.Infrastructure.Exporting.Documents;

/// <summary>A printable invoice or receipt for a patient account.</summary>
public class InvoiceDocument(Invoice invoice, Practice? practice, IReadOnlyList<Payment> payments) : IDocument
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
                header, practice,
                invoice.Status == Domain.Enums.InvoiceStatus.Paid ? "Receipt" : "Invoice",
                invoice.InvoiceNumber));

            page.Footer().Element(footer => PdfTheme.Footer(footer, practice, practice?.LegalName));

            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Item().Element(Parties);
                column.Item().PaddingTop(14).Element(Lines);
                column.Item().PaddingTop(10).Element(Totals);

                if (payments.Count > 0)
                    column.Item().PaddingTop(12).Element(PaymentHistory);

                column.Item().PaddingTop(14).Element(Terms);
            });
        });
    }

    private void Parties(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(left =>
            {
                left.Item().Text("BILL TO").Style(PdfTheme.Label);
                left.Item().Text(invoice.Patient?.Name.Full ?? "Patient").Style(PdfTheme.Body).Bold();

                if (invoice.Patient is not null)
                {
                    foreach (var line in invoice.Patient.Address.ToLines())
                        left.Item().Text(line).Style(PdfTheme.Small);

                    left.Item().PaddingTop(3).Text($"Patient number {invoice.Patient.PatientNumber}")
                        .Style(PdfTheme.Small);
                }

                if (invoice.GuarantorPatient is not null)
                    left.Item().PaddingTop(3)
                        .Text($"Account holder: {invoice.GuarantorPatient.Name.Display}").Style(PdfTheme.Small);
            });

            row.ConstantItem(200).Column(right =>
            {
                PdfTheme.Field(right, "Invoice date", invoice.IssueDate.ToString("d MMMM yyyy"));
                PdfTheme.Field(right, "Due date", invoice.DueDate.ToString("d MMMM yyyy"));
                PdfTheme.Field(right, "Provider", invoice.Provider?.DisplayName);
                PdfTheme.Field(right, "Location", invoice.Location?.Name);
                PdfTheme.Field(right, "Status", PdfTheme.Humanise(invoice.Status));

                if (invoice.IsOverdue)
                    right.Item().PaddingTop(4).Text($"{invoice.DaysOverdue} days overdue")
                        .Style(PdfTheme.Small).FontColor(PdfTheme.Danger).Bold();
            });
        });
    }

    private void Lines(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(52);   // date
                columns.ConstantColumn(44);   // code
                columns.RelativeColumn(3);    // description
                columns.ConstantColumn(34);   // tooth
                columns.ConstantColumn(30);   // qty
                columns.ConstantColumn(56);   // unit
                columns.ConstantColumn(50);   // discount
                columns.ConstantColumn(60);   // total
            });

            table.Header(header =>
            {
                foreach (var (text, align) in new[]
                {
                    ("Date", ColumnAlignment.Left), ("Code", ColumnAlignment.Left),
                    ("Treatment", ColumnAlignment.Left), ("Tooth", ColumnAlignment.Left),
                    ("Qty", ColumnAlignment.Right), ("Fee", ColumnAlignment.Right),
                    ("Discount", ColumnAlignment.Right), ("Total", ColumnAlignment.Right)
                })
                {
                    var cell = header.Cell().Background(PdfTheme.Surface)
                        .BorderBottom(1).BorderColor(PdfTheme.LineStrong)
                        .PaddingVertical(4).PaddingHorizontal(3);

                    (align == ColumnAlignment.Right ? cell.AlignRight() : cell.AlignLeft())
                        .Text(text).Style(PdfTheme.TableHeader);
                }
            });

            foreach (var line in invoice.Lines.OrderBy(l => l.Sequence))
            {
                Cell(table).Text(line.ServiceDate.ToString("dd/MM/yy")).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                Cell(table).Text(line.ProcedureCode?.Code ?? "-").Style(PdfTheme.Small).FontColor(PdfTheme.Ink);

                Cell(table).Column(description =>
                {
                    description.Item().Text(line.Description).Style(PdfTheme.Body).FontSize(PdfTheme.SmallSize);
                    if (line.InsurancePortion > 0)
                        description.Item().Text($"Estimated insurance {PdfTheme.Money(line.InsurancePortion, Symbol)}")
                            .Style(PdfTheme.Small).FontSize(6.5f);
                });

                Cell(table).Text(line.Tooth?.FdiNumber.ToString() ?? "-").Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                Cell(table).AlignRight().Text(line.Quantity.ToString("0.##")).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                Cell(table).AlignRight().Text(PdfTheme.Money(line.UnitPrice, Symbol)).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                Cell(table).AlignRight().Text(line.DiscountAmount > 0 ? PdfTheme.Money(line.DiscountAmount, Symbol) : "-")
                    .Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                Cell(table).AlignRight().Text(PdfTheme.Money(line.LineTotal, Symbol)).Style(PdfTheme.Small).Bold().FontColor(PdfTheme.Ink);
            }
        });
    }

    private static IContainer Cell(TableDescriptor table) =>
        table.Cell().BorderBottom(0.5f).BorderColor(PdfTheme.Line).PaddingVertical(3).PaddingHorizontal(3);

    private void Totals(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem();

            row.ConstantItem(240).Element(panel => PdfTheme.Panel(panel).Column(column =>
            {
                Line(column, "Subtotal", PdfTheme.Money(invoice.Subtotal, Symbol));

                if (invoice.DiscountAmount > 0)
                    Line(column, "Discount", "-" + PdfTheme.Money(invoice.DiscountAmount, Symbol));

                if (invoice.TaxAmount > 0)
                    Line(column, "Tax", PdfTheme.Money(invoice.TaxAmount, Symbol));

                column.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(PdfTheme.LineStrong);
                Line(column, "Total", PdfTheme.Money(invoice.Total, Symbol), bold: true);

                if (invoice.InsuranceEstimate > 0)
                    Line(column, "Estimated insurance", "-" + PdfTheme.Money(invoice.InsuranceEstimate, Symbol));

                if (invoice.AmountPaid > 0)
                    Line(column, "Paid", "-" + PdfTheme.Money(invoice.AmountPaid, Symbol));

                if (invoice.WriteOffAmount > 0)
                    Line(column, "Written off", "-" + PdfTheme.Money(invoice.WriteOffAmount, Symbol));

                column.Item().PaddingVertical(3).LineHorizontal(1).LineColor(PdfTheme.LineStrong);

                column.Item().Row(balance =>
                {
                    balance.RelativeItem().Text("Balance due").Style(PdfTheme.Body).Bold();
                    balance.ConstantItem(90).AlignRight()
                        .Text(PdfTheme.Money(invoice.Balance, Symbol))
                        .Style(PdfTheme.Body).FontSize(13).Bold()
                        .FontColor(invoice.Balance > 0 ? PdfTheme.Danger : PdfTheme.Success);
                });
            }));
        });
    }

    private static void Line(ColumnDescriptor column, string label, string value, bool bold = false)
    {
        column.Item().PaddingVertical(1).Row(row =>
        {
            var labelText = row.RelativeItem().Text(label).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
            if (bold) labelText.Bold();

            var valueText = row.ConstantItem(90).AlignRight().Text(value).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
            if (bold) valueText.Bold();
        });
    }

    private void PaymentHistory(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Text("Payments received").Style(PdfTheme.Label);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);
                    columns.RelativeColumn();
                    columns.ConstantColumn(120);
                    columns.ConstantColumn(70);
                });

                foreach (var payment in payments.OrderBy(p => p.PaymentDate))
                {
                    Cell(table).Text(payment.PaymentDate.ToString("dd/MM/yyyy")).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                    Cell(table).Text(PdfTheme.Humanise(payment.Method)).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                    Cell(table).Text(payment.ReferenceNumber ?? payment.PaymentNumber).Style(PdfTheme.Small);
                    Cell(table).AlignRight().Text(PdfTheme.Money(payment.Amount, Symbol)).Style(PdfTheme.Small).FontColor(PdfTheme.Ink);
                }
            });
        });
    }

    private void Terms(IContainer container)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(invoice.TermsText))
                column.Item().Text(invoice.TermsText).Style(PdfTheme.Small);
            else if (practice?.InvoiceFooterText is { Length: > 0 })
                column.Item().Text(practice.InvoiceFooterText).Style(PdfTheme.Small);

            if (invoice.Balance > 0 && !string.IsNullOrWhiteSpace(practice?.BankDetails))
                column.Item().PaddingTop(4).Text($"Payment details: {practice.BankDetails}").Style(PdfTheme.Small);

            if (!string.IsNullOrWhiteSpace(invoice.Notes))
                column.Item().PaddingTop(4).Text(invoice.Notes).Style(PdfTheme.Small);
        });
    }

    private enum ColumnAlignment { Left, Right }
}
