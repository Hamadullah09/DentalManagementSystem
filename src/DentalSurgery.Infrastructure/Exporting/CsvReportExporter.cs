using DentalSurgery.Application.Exporting;
using System.Globalization;
using System.Text;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>
/// RFC 4180 CSV. Values are written unformatted so a spreadsheet can treat
/// numbers as numbers; a header block carries the report context.
/// </summary>
public class CsvReportExporter : IReportExporter
{
    public ExportFormat Format => ExportFormat.Csv;
    public string ContentType => "text/csv";
    public string FileExtension => "csv";

    public byte[] Render(ReportTable table)
    {
        var builder = new StringBuilder();

        builder.AppendLine(Escape(table.Title));
        if (!string.IsNullOrWhiteSpace(table.Subtitle)) builder.AppendLine(Escape(table.Subtitle));
        builder.AppendLine(Escape(table.PeriodLabel));
        builder.AppendLine(Escape($"Generated {table.GeneratedAtUtc.ToLocalTime():d MMMM yyyy HH:mm}" +
                                  (table.GeneratedBy is null ? "" : $" by {table.GeneratedBy}")));
        builder.AppendLine();

        if (table.Metrics.Count > 0)
        {
            foreach (var metric in table.Metrics)
                builder.AppendLine($"{Escape(metric.Label)},{Escape(metric.Value)}");
            builder.AppendLine();
        }

        builder.AppendLine(string.Join(",", table.Columns.Select(c => Escape(c.Header))));

        foreach (var row in table.Rows)
        {
            var cells = new string[table.Columns.Count];
            for (var i = 0; i < table.Columns.Count; i++)
                cells[i] = Escape(Raw(i < row.Length ? row[i] : null));
            builder.AppendLine(string.Join(",", cells));
        }

        if (table.HasTotals && table.Rows.Count > 0)
        {
            var totals = table.ComputeTotals();
            var cells = new string[table.Columns.Count];
            cells[0] = Escape($"Total ({table.Rows.Count} rows)");
            for (var i = 1; i < table.Columns.Count; i++)
                cells[i] = totals.TryGetValue(i, out var value)
                    ? value.ToString("0.##", CultureInfo.InvariantCulture)
                    : string.Empty;
            builder.AppendLine(string.Join(",", cells));
        }

        if (!string.IsNullOrWhiteSpace(table.Footer))
        {
            builder.AppendLine();
            builder.AppendLine(Escape(table.Footer));
        }

        // The BOM stops spreadsheet applications mangling accented names.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    /// <summary>Machine-readable form: ISO dates, unformatted numbers.</summary>
    private static string Raw(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("0.##", CultureInfo.InvariantCulture),
        double d => d.ToString("0.##", CultureInfo.InvariantCulture),
        float f => f.ToString("0.##", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm"),
        TimeSpan t => t.ToString(@"hh\:mm"),
        bool b => b ? "Yes" : "No",
        Enum e => Humanise(e.ToString()),
        _ => value.ToString() ?? string.Empty
    };

    internal static string Humanise(string pascalCase) =>
        System.Text.RegularExpressions.Regex.Replace(pascalCase, "(?<!^)([A-Z])", " $1");

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"') ||
                          value.Contains('\n') || value.Contains('\r');

        return needsQuotes ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}

/// <summary>Machine-readable export for downstream systems.</summary>
public class JsonReportExporter : IReportExporter
{
    private static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ExportFormat Format => ExportFormat.Json;
    public string ContentType => "application/json";
    public string FileExtension => "json";

    public byte[] Render(ReportTable table)
    {
        var rows = table.Rows.Select(row =>
        {
            var record = new Dictionary<string, object?>();
            for (var i = 0; i < table.Columns.Count; i++)
                record[table.Columns[i].Header] = Normalise(i < row.Length ? row[i] : null);
            return record;
        }).ToList();

        var payload = new
        {
            table.Key,
            table.Title,
            table.Subtitle,
            Period = new { table.From, table.To, Label = table.PeriodLabel },
            table.GeneratedAtUtc,
            table.GeneratedBy,
            Metrics = table.Metrics,
            Columns = table.Columns.Select(c => new { c.Header, Type = c.Type.ToString() }),
            RowCount = rows.Count,
            Rows = rows
        };

        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload, Options);
    }

    private static object? Normalise(object? value) => value switch
    {
        Enum e => e.ToString(),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        TimeSpan t => t.ToString(@"hh\:mm"),
        _ => value
    };
}
