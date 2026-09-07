using ClosedXML.Excel;
using DentalSurgery.Application.Exporting;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>
/// Produces a formatted workbook: a context header, headline metrics, then the
/// data as a real Excel table with typed cells, so totals and pivots work
/// without the recipient having to re-type anything.
/// </summary>
public class ExcelReportExporter : IReportExporter
{
    private const string Money = "£#,##0.00;[Red]-£#,##0.00";
    private const string Number = "#,##0.##";
    private const string Integer = "#,##0";
    private const string Percent = "0.0\"%\"";
    private const string DateFormat = "dd/mm/yyyy";
    private const string DateTimeFormat = "dd/mm/yyyy hh:mm";

    private static readonly XLColor Ink = XLColor.FromHtml("#1B2430");
    private static readonly XLColor Muted = XLColor.FromHtml("#5F6B7A");
    private static readonly XLColor Accent = XLColor.FromHtml("#1266D6");
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#EEF1F5");
    private static readonly XLColor TotalFill = XLColor.FromHtml("#E7F0FD");

    public ExportFormat Format => ExportFormat.Excel;
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";

    public byte[] Render(ReportTable table)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName(table.Title));

        var row = 1;
        var lastColumn = Math.Max(1, table.Columns.Count);

        // ---- context header -------------------------------------------------
        sheet.Cell(row, 1).Value = table.Title;
        sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(Ink);
        sheet.Range(row, 1, row, lastColumn).Merge();
        row++;

        if (!string.IsNullOrWhiteSpace(table.Subtitle))
        {
            sheet.Cell(row, 1).Value = table.Subtitle;
            sheet.Cell(row, 1).Style.Font.SetFontColor(Muted);
            sheet.Range(row, 1, row, lastColumn).Merge();
            row++;
        }

        sheet.Cell(row, 1).Value = table.PeriodLabel;
        sheet.Cell(row, 1).Style.Font.SetFontColor(Muted).Font.SetItalic();
        sheet.Range(row, 1, row, lastColumn).Merge();
        row++;

        sheet.Cell(row, 1).Value = $"Generated {table.GeneratedAtUtc.ToLocalTime():d MMMM yyyy HH:mm}" +
                                   (table.GeneratedBy is null ? "" : $" by {table.GeneratedBy}");
        sheet.Cell(row, 1).Style.Font.SetFontColor(Muted).Font.SetFontSize(9);
        sheet.Range(row, 1, row, lastColumn).Merge();
        row += 2;

        // ---- headline metrics -----------------------------------------------
        if (table.Metrics.Count > 0)
        {
            foreach (var metric in table.Metrics)
            {
                sheet.Cell(row, 1).Value = metric.Label;
                sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontColor(Muted);
                sheet.Cell(row, 2).Value = metric.Value;
                sheet.Cell(row, 2).Style.Font.SetFontColor(Accent).Font.SetBold();
                if (!string.IsNullOrWhiteSpace(metric.Caption))
                {
                    sheet.Cell(row, 3).Value = metric.Caption;
                    sheet.Cell(row, 3).Style.Font.SetFontColor(Muted).Font.SetFontSize(9);
                }
                row++;
            }
            row++;
        }

        if (table.Columns.Count == 0)
        {
            sheet.Columns().AdjustToContents();
            return Save(workbook);
        }

        // ---- column headers --------------------------------------------------
        var headerRow = row;
        for (var column = 0; column < table.Columns.Count; column++)
        {
            var cell = sheet.Cell(headerRow, column + 1);
            cell.Value = table.Columns[column].Header;
            cell.Style.Font.SetBold().Font.SetFontColor(Ink);
            cell.Style.Fill.SetBackgroundColor(HeaderFill);
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Alignment.SetHorizontal(Align(table.Columns[column]));

            if (!string.IsNullOrWhiteSpace(table.Columns[column].Note))
                cell.CreateComment().AddText(table.Columns[column].Note!);
        }
        row++;

        // ---- data ------------------------------------------------------------
        var firstDataRow = row;
        foreach (var dataRow in table.Rows)
        {
            for (var column = 0; column < table.Columns.Count; column++)
            {
                var cell = sheet.Cell(row, column + 1);
                WriteCell(cell, table.Columns[column], column < dataRow.Length ? dataRow[column] : null);
                cell.Style.Alignment.SetHorizontal(Align(table.Columns[column]));
            }
            row++;
        }

        var lastDataRow = Math.Max(firstDataRow, row - 1);

        if (table.Rows.Count > 0)
        {
            var range = sheet.Range(headerRow, 1, lastDataRow, table.Columns.Count);
            range.SetAutoFilter();
            sheet.SheetView.Freeze(headerRow, 0);
        }

        // ---- totals ----------------------------------------------------------
        if (table.HasTotals && table.Rows.Count > 0)
        {
            var totals = table.ComputeTotals();
            sheet.Cell(row, 1).Value = $"Total ({table.Rows.Count} rows)";
            sheet.Cell(row, 1).Style.Font.SetBold();

            for (var column = 0; column < table.Columns.Count; column++)
            {
                var cell = sheet.Cell(row, column + 1);
                cell.Style.Fill.SetBackgroundColor(TotalFill);
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Font.SetBold();

                if (!totals.TryGetValue(column, out var total)) continue;

                // A formula rather than a literal, so the figure survives filtering.
                var letter = sheet.Cell(1, column + 1).Address.ColumnLetter;
                cell.FormulaA1 = $"SUBTOTAL(109,{letter}{firstDataRow}:{letter}{lastDataRow})";
                cell.Style.NumberFormat.Format = FormatFor(table.Columns[column].Type);
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }
            row++;
        }

        if (!string.IsNullOrWhiteSpace(table.Footer))
        {
            row++;
            sheet.Cell(row, 1).Value = table.Footer;
            sheet.Cell(row, 1).Style.Font.SetFontColor(Muted).Font.SetFontSize(9);
            sheet.Range(row, 1, row, lastColumn).Merge();
        }

        sheet.Columns(1, table.Columns.Count).AdjustToContents(8d, 46d);
        sheet.PageSetup.PageOrientation = table.Landscape
            ? XLPageOrientation.Landscape
            : XLPageOrientation.Portrait;
        sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
        sheet.PageSetup.FitToPages(1, 0);

        return Save(workbook);
    }

    private static void WriteCell(IXLCell cell, ReportColumn column, object? value)
    {
        if (value is null) return;

        switch (value)
        {
            case decimal d:
                cell.Value = d;
                cell.Style.NumberFormat.Format = FormatFor(column.Type);
                break;
            case double dbl:
                cell.Value = dbl;
                cell.Style.NumberFormat.Format = FormatFor(column.Type);
                break;
            case int i:
                cell.Value = i;
                cell.Style.NumberFormat.Format = column.Type == ReportColumnType.Money ? Money : Integer;
                break;
            case long l:
                cell.Value = l;
                cell.Style.NumberFormat.Format = Integer;
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = DateFormat;
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.NumberFormat.Format = DateTimeFormat;
                break;
            case TimeSpan time:
                cell.Value = time.ToString(@"hh\:mm");
                break;
            case bool flag:
                cell.Value = flag ? "Yes" : "No";
                break;
            case Enum enumeration:
                cell.Value = CsvReportExporter.Humanise(enumeration.ToString());
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static string FormatFor(ReportColumnType type) => type switch
    {
        ReportColumnType.Money => Money,
        ReportColumnType.Percent => Percent,
        ReportColumnType.Integer => Integer,
        ReportColumnType.Number => Number,
        _ => Number
    };

    private static XLAlignmentHorizontalValues Align(ReportColumn column) => column.EffectiveAlign switch
    {
        ColumnAlign.Right => XLAlignmentHorizontalValues.Right,
        ColumnAlign.Centre => XLAlignmentHorizontalValues.Center,
        _ => XLAlignmentHorizontalValues.Left
    };

    /// <summary>Excel rejects some characters in sheet names and caps them at 31.</summary>
    private static string SheetName(string title)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var cleaned = new string(title.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "Report";
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
