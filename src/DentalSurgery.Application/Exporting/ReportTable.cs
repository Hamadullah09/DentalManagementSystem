namespace DentalSurgery.Application.Exporting;

public enum ExportFormat { Csv = 0, Excel = 1, Pdf = 2, Json = 3 }

public enum ReportColumnType { Text = 0, Number = 1, Money = 2, Date = 3, DateTime = 4, Percent = 5, Boolean = 6, Integer = 7 }

public enum ColumnAlign { Left = 0, Right = 1, Centre = 2 }

/// <summary>One column of a report, describing how its cells should be rendered.</summary>
public record ReportColumn(
    string Header,
    ReportColumnType Type = ReportColumnType.Text,
    ColumnAlign Align = ColumnAlign.Left,
    bool Total = false,
    double Width = 1,
    string? Note = null)
{
    public ColumnAlign EffectiveAlign => Align != ColumnAlign.Left
        ? Align
        : Type switch
        {
            ReportColumnType.Number or ReportColumnType.Money or
            ReportColumnType.Percent or ReportColumnType.Integer => ColumnAlign.Right,
            ReportColumnType.Boolean => ColumnAlign.Centre,
            _ => ColumnAlign.Left
        };
}

/// <summary>A headline figure shown above the table.</summary>
public record ReportMetric(string Label, string Value, string? Caption = null);

/// <summary>
/// A rendered report, independent of output format. Every exporter consumes this
/// shape, so a report is defined once and can be produced as CSV, a spreadsheet,
/// a PDF or JSON without duplicating the query.
/// </summary>
public record ReportTable
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string? GeneratedBy { get; init; }

    public IReadOnlyList<ReportColumn> Columns { get; init; } = Array.Empty<ReportColumn>();
    public IReadOnlyList<object?[]> Rows { get; init; } = Array.Empty<object?[]>();
    public IReadOnlyList<ReportMetric> Metrics { get; init; } = Array.Empty<ReportMetric>();
    public string? Footer { get; init; }

    /// <summary>True when the report is landscape-shaped and should be paged that way.</summary>
    public bool Landscape => Columns.Count > 7;

    public bool HasTotals => Columns.Any(c => c.Total);

    public string PeriodLabel => (From, To) switch
    {
        (null, null) => "As at " + GeneratedAtUtc.ToLocalTime().ToString("d MMMM yyyy"),
        ({ } f, null) => "From " + f.ToString("d MMMM yyyy"),
        (null, { } t) => "To " + t.ToString("d MMMM yyyy"),
        ({ } f, { } t) when f == t => f.ToString("d MMMM yyyy"),
        ({ } f, { } t) => $"{f:d MMMM yyyy} to {t:d MMMM yyyy}"
    };

    /// <summary>Column totals, computed once and reused by every exporter.</summary>
    public IReadOnlyDictionary<int, decimal> ComputeTotals()
    {
        var totals = new Dictionary<int, decimal>();

        for (var column = 0; column < Columns.Count; column++)
        {
            if (!Columns[column].Total) continue;

            decimal sum = 0;
            foreach (var row in Rows)
            {
                if (column >= row.Length) continue;
                sum += row[column] switch
                {
                    decimal d => d,
                    double dbl => (decimal)dbl,
                    int i => i,
                    long l => l,
                    _ => 0m
                };
            }
            totals[column] = sum;
        }

        return totals;
    }

    /// <summary>A file name stem, safe on every platform.</summary>
    public string FileStem
    {
        get
        {
            var stem = string.IsNullOrWhiteSpace(Key) ? Title : Key;
            var cleaned = new string(stem.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());
            while (cleaned.Contains("--")) cleaned = cleaned.Replace("--", "-");
            cleaned = cleaned.Trim('-');

            var period = (From, To) switch
            {
                ({ } f, { } t) => $"-{f:yyyyMMdd}-{t:yyyyMMdd}",
                ({ } f, null) => $"-{f:yyyyMMdd}",
                (null, { } t) => $"-{t:yyyyMMdd}",
                _ => $"-{GeneratedAtUtc:yyyyMMdd}"
            };

            return cleaned + period;
        }
    }
}

/// <summary>Renders a <see cref="ReportTable"/> into a downloadable file.</summary>
public interface IReportExporter
{
    ExportFormat Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    byte[] Render(ReportTable table);
}

/// <summary>Metadata for a report the export screen can offer.</summary>
public record ReportDefinition(
    string Key,
    string Name,
    string Description,
    string Category,
    bool NeedsDateRange = true)
{
    public static readonly ExportFormat[] AllFormats =
        { ExportFormat.Csv, ExportFormat.Excel, ExportFormat.Pdf, ExportFormat.Json };
}
