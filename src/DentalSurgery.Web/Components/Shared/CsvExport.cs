using Microsoft.JSInterop;
using System.Globalization;
using System.Text;

namespace DentalSurgery.Web.Components.Shared;

/// <summary>Builds RFC 4180 CSV in memory and hands it to the browser to save.</summary>
public static class CsvExport
{
    public static string Build<T>(IEnumerable<T> rows, params (string Header, Func<T, string> Value)[] columns)
    {
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(",", columns.Select(c => Escape(c.Header))));

        foreach (var row in rows)
            builder.AppendLine(string.Join(",", columns.Select(c => Escape(c.Value(row)))));

        return builder.ToString();
    }

    /// <summary>
    /// Quotes a field when it contains a delimiter, quote or newline, doubling
    /// any embedded quotes as the format requires.
    /// </summary>
    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"') ||
                          value.Contains('\n') || value.Contains('\r');

        return needsQuotes ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    public static async Task DownloadAsync(IJSRuntime js, string fileName, string csv)
    {
        try
        {
            // The BOM keeps spreadsheet applications from mangling accented names.
            await js.InvokeVoidAsync("dentalApp.downloadText", fileName, "text/csv;charset=utf-8", "﻿" + csv);
        }
        catch (JSException)
        {
            // The browser blocked the download; nothing further to do here.
        }
    }

    public static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    public static string Date(DateOnly? value) => value?.ToString("yyyy-MM-dd") ?? string.Empty;
    public static string DateTimeIso(DateTime? value) => value?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
}
