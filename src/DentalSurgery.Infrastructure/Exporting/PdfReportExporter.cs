using DentalSurgery.Application.Exporting;
using DentalSurgery.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>
/// Renders a report as a printable PDF on the practice letterhead, with a
/// repeating header row, headline metrics and a totals band.
/// </summary>
public class PdfReportExporter(IPracticeAccessor practice) : IReportExporter
{
    public ExportFormat Format => ExportFormat.Pdf;
    public string ContentType => "application/pdf";
    public string FileExtension => "pdf";

    public byte[] Render(ReportTable table)
    {
        var letterhead = practice.Current;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(table.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(PdfTheme.Body);

                page.Header().Element(header => PdfTheme.Letterhead(header, letterhead, table.Title, table.PeriodLabel));
                page.Footer().Element(footer => PdfTheme.Footer(footer, letterhead,
                    $"Generated {table.GeneratedAtUtc.ToLocalTime():d MMM yyyy HH:mm}" +
                    (table.GeneratedBy is null ? "" : $" by {table.GeneratedBy}")));

                page.Content().PaddingVertical(10).Column(column =>
                {
                    if (!string.IsNullOrWhiteSpace(table.Subtitle))
                        column.Item().PaddingBottom(8).Text(table.Subtitle).Style(PdfTheme.Small);

                    if (table.Metrics.Count > 0)
                        column.Item().PaddingBottom(10).Element(e => Metrics(e, table));

                    if (table.Columns.Count == 0 || table.Rows.Count == 0)
                    {
                        column.Item().PaddingTop(20).AlignCenter()
                            .Text("No data for this period.").Style(PdfTheme.Small).FontSize(10);
                        return;
                    }

                    column.Item().Element(e => Table(e, table));

                    if (!string.IsNullOrWhiteSpace(table.Footer))
                        column.Item().PaddingTop(10).Text(table.Footer).Style(PdfTheme.Small);
                });
            });
        }).GeneratePdf();
    }

    private static void Metrics(IContainer container, ReportTable table)
    {
        // Three metric tiles per row keeps them legible on A4.
        var chunks = table.Metrics.Chunk(3).ToList();

        container.Column(column =>
        {
            foreach (var chunk in chunks)
            {
                column.Item().PaddingBottom(4).Row(row =>
                {
                    foreach (var metric in chunk)
                    {
                        row.RelativeItem().PaddingRight(6).Element(cell =>
                            PdfTheme.Panel(cell, PdfTheme.AccentSoft).Column(inner =>
                            {
                                inner.Item().Text(metric.Label.ToUpperInvariant()).Style(PdfTheme.Label);
                                inner.Item().Text(metric.Value).Style(PdfTheme.Body).FontSize(13).Bold();
                                if (!string.IsNullOrWhiteSpace(metric.Caption))
                                    inner.Item().Text(metric.Caption).Style(PdfTheme.Small).FontSize(7);
                            }));
                    }

                    // Keep the last row's tiles the same width as a full row.
                    for (var filler = chunk.Length; filler < 3; filler++)
                        row.RelativeItem().PaddingRight(6);
                });
            }
        });
    }

    private static void Table(IContainer container, ReportTable report)
    {
        var totals = report.HasTotals ? report.ComputeTotals() : new Dictionary<int, decimal>();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var column in report.Columns)
                    columns.RelativeColumn((float)Math.Max(0.5, column.Width));
            });

            table.Header(header =>
            {
                foreach (var column in report.Columns)
                {
                    header.Cell()
                        .Background(PdfTheme.Surface)
                        .BorderBottom(1).BorderColor(PdfTheme.LineStrong)
                        .PaddingVertical(4).PaddingHorizontal(3)
                        .AlignedBy(column.EffectiveAlign)
                        .Text(column.Header).Style(PdfTheme.TableHeader);
                }
            });

            var striped = false;
            foreach (var row in report.Rows)
            {
                striped = !striped;

                for (var index = 0; index < report.Columns.Count; index++)
                {
                    var column = report.Columns[index];
                    var value = index < row.Length ? row[index] : null;

                    table.Cell()
                        .Background(striped ? Colors.White : PdfTheme.Surface)
                        .BorderBottom(0.5f).BorderColor(PdfTheme.Line)
                        .PaddingVertical(3).PaddingHorizontal(3)
                        .AlignedBy(column.EffectiveAlign)
                        .Text(FormatCell(value, column.Type))
                        .Style(PdfTheme.Body).FontSize(PdfTheme.SmallSize);
                }
            }

            if (report.HasTotals && report.Rows.Count > 0)
            {
                for (var index = 0; index < report.Columns.Count; index++)
                {
                    var column = report.Columns[index];
                    var text = index == 0
                        ? $"Total ({report.Rows.Count} rows)"
                        : totals.TryGetValue(index, out var total) ? FormatCell(total, column.Type) : string.Empty;

                    table.Cell()
                        .Background(PdfTheme.AccentSoft)
                        .BorderTop(1).BorderColor(PdfTheme.LineStrong)
                        .PaddingVertical(4).PaddingHorizontal(3)
                        .AlignedBy(column.EffectiveAlign)
                        .Text(text).Style(PdfTheme.Body).FontSize(PdfTheme.SmallSize).Bold();
                }
            }
        });
    }

    /// <summary>Formats a cell for print. Exposed so the formatting can be tested directly.</summary>
    public static string FormatCell(object? value, ReportColumnType type) => value switch
    {
        null => string.Empty,
        decimal d => type switch
        {
            ReportColumnType.Money => PdfTheme.Money(d),
            ReportColumnType.Percent => $"{d:0.#}%",
            ReportColumnType.Integer => d.ToString("N0"),
            _ => d.ToString("N2")
        },
        double dbl => type == ReportColumnType.Percent ? $"{dbl:0.#}%" : dbl.ToString("N2"),
        int i => type == ReportColumnType.Money ? PdfTheme.Money(i) : i.ToString("N0"),
        long l => l.ToString("N0"),
        DateOnly date => date.ToString("dd/MM/yyyy"),
        DateTime dateTime => type == ReportColumnType.Date
            ? dateTime.ToString("dd/MM/yyyy")
            : dateTime.ToString("dd/MM/yyyy HH:mm"),
        TimeSpan time => time.ToString(@"hh\:mm"),
        bool flag => flag ? "Yes" : "No",
        Enum enumeration => PdfTheme.Humanise(enumeration),
        _ => value.ToString() ?? string.Empty
    };
}

internal static class PdfAlignmentExtensions
{
    public static IContainer AlignedBy(this IContainer container, ColumnAlign align) => align switch
    {
        ColumnAlign.Right => container.AlignRight(),
        ColumnAlign.Centre => container.AlignCenter(),
        _ => container.AlignLeft()
    };
}

/// <summary>
/// Supplies the practice record for letterheads without every document having
/// to query for it. Cached because it changes at most a few times a year.
/// </summary>
public interface IPracticeAccessor
{
    Practice? Current { get; }
    void Invalidate();
}
