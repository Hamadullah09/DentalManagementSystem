using ClosedXML.Excel;
using DentalSurgery.Application.Exporting;
using DentalSurgery.Infrastructure.Exporting;
using System.Text;
using Xunit;

namespace DentalSurgery.Tests;

public class ReportTableTests
{
    private static ReportTable Sample() => new()
    {
        Key = "production-by-category",
        Title = "Production by category",
        Subtitle = "Completed treatment.",
        From = new DateOnly(2026, 1, 1),
        To = new DateOnly(2026, 3, 31),
        GeneratedAtUtc = new DateTime(2026, 4, 1, 9, 30, 0, DateTimeKind.Utc),
        GeneratedBy = "Test user",
        Columns = new[]
        {
            new ReportColumn("Category"),
            new ReportColumn("Procedures", ReportColumnType.Integer, Total: true),
            new ReportColumn("Value", ReportColumnType.Money, Total: true)
        },
        Rows = new List<object?[]>
        {
            new object?[] { "Restorative", 12, 1450.50m },
            new object?[] { "Preventive", 30, 2100m },
            new object?[] { "Endodontics", 3, 1560m }
        },
        Metrics = new[] { new ReportMetric("Gross production", "£5,110.50") }
    };

    [Fact]
    public void Totals_are_summed_only_for_columns_marked_total()
    {
        var totals = Sample().ComputeTotals();

        Assert.Equal(2, totals.Count);
        Assert.False(totals.ContainsKey(0));
        Assert.Equal(45m, totals[1]);
        Assert.Equal(5110.50m, totals[2]);
    }

    [Fact]
    public void An_empty_report_totals_to_zero_rather_than_failing()
    {
        var table = Sample() with { Rows = Array.Empty<object?[]>() };
        var totals = table.ComputeTotals();

        Assert.Equal(0m, totals[1]);
        Assert.Equal(0m, totals[2]);
    }

    [Fact]
    public void Short_rows_do_not_break_the_totals()
    {
        // A builder that omits trailing nulls must not throw.
        var table = Sample() with { Rows = new List<object?[]> { new object?[] { "Restorative" } } };
        var totals = table.ComputeTotals();

        Assert.Equal(0m, totals[2]);
    }

    [Fact]
    public void The_file_stem_is_slugged_and_carries_the_period()
    {
        Assert.Equal("production-by-category-20260101-20260331", Sample().FileStem);
    }

    [Fact]
    public void A_report_with_no_period_falls_back_to_the_generation_date()
    {
        var table = Sample() with { From = null, To = null };
        Assert.Equal("production-by-category-20260401", table.FileStem);
        Assert.StartsWith("As at", table.PeriodLabel);
    }

    [Fact]
    public void A_single_day_period_reads_as_one_date()
    {
        var day = new DateOnly(2026, 5, 4);
        var table = Sample() with { From = day, To = day };
        Assert.Equal("4 May 2026", table.PeriodLabel);
    }

    [Fact]
    public void Wide_reports_are_laid_out_landscape()
    {
        Assert.False(Sample().Landscape);

        var wide = Sample() with
        {
            Columns = Enumerable.Range(0, 9).Select(i => new ReportColumn($"Column {i}")).ToList()
        };
        Assert.True(wide.Landscape);
    }

    [Fact]
    public void Money_and_number_columns_right_align_by_default()
    {
        Assert.Equal(ColumnAlign.Left, new ReportColumn("Name").EffectiveAlign);
        Assert.Equal(ColumnAlign.Right, new ReportColumn("Fee", ReportColumnType.Money).EffectiveAlign);
        Assert.Equal(ColumnAlign.Right, new ReportColumn("Count", ReportColumnType.Integer).EffectiveAlign);
        Assert.Equal(ColumnAlign.Centre, new ReportColumn("Flag", ReportColumnType.Boolean).EffectiveAlign);
    }

    [Fact]
    public void An_explicit_alignment_wins_over_the_type_default()
    {
        var column = new ReportColumn("Fee", ReportColumnType.Money, ColumnAlign.Centre);
        Assert.Equal(ColumnAlign.Centre, column.EffectiveAlign);
    }
}

public class CsvReportExporterTests
{
    private readonly CsvReportExporter _exporter = new();

    private static ReportTable Table(params object?[][] rows) => new()
    {
        Key = "test", Title = "Test report",
        Columns = new[]
        {
            new ReportColumn("Name"),
            new ReportColumn("Amount", ReportColumnType.Money, Total: true),
            new ReportColumn("Date", ReportColumnType.Date)
        },
        Rows = rows
    };

    private string Render(ReportTable table) =>
        Encoding.UTF8.GetString(_exporter.Render(table)).TrimStart('﻿');

    [Fact]
    public void The_output_starts_with_a_byte_order_mark()
    {
        var bytes = _exporter.Render(Table());
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
    }

    [Fact]
    public void Fields_containing_a_comma_are_quoted()
    {
        var csv = Render(Table(new object?[] { "Abernathy, Bertrand", 100m, null }));
        Assert.Contains("\"Abernathy, Bertrand\"", csv);
    }

    [Fact]
    public void Embedded_quotes_are_doubled()
    {
        var csv = Render(Table(new object?[] { "The \"main\" surgery", 10m, null }));
        Assert.Contains("\"The \"\"main\"\" surgery\"", csv);
    }

    [Fact]
    public void Newlines_inside_a_field_are_kept_within_quotes()
    {
        var csv = Render(Table(new object?[] { "Line one\nLine two", 10m, null }));
        Assert.Contains("\"Line one\nLine two\"", csv);
    }

    [Fact]
    public void Dates_are_written_in_iso_form()
    {
        var csv = Render(Table(new object?[] { "Test", 10m, new DateOnly(2026, 7, 4) }));
        Assert.Contains("2026-07-04", csv);
    }

    [Fact]
    public void Money_is_unformatted_so_a_spreadsheet_reads_it_as_a_number()
    {
        var csv = Render(Table(new object?[] { "Test", 1234.5m, null }));
        Assert.Contains("1234.5", csv);
        Assert.DoesNotContain("£", csv);
        Assert.DoesNotContain("1,234", csv);
    }

    [Fact]
    public void Booleans_read_as_yes_and_no()
    {
        var table = new ReportTable
        {
            Title = "Flags",
            Columns = new[] { new ReportColumn("Flag", ReportColumnType.Boolean) },
            Rows = new List<object?[]> { new object?[] { true }, new object?[] { false } }
        };

        var csv = Encoding.UTF8.GetString(_exporter.Render(table));
        Assert.Contains("Yes", csv);
        Assert.Contains("No", csv);
    }

    [Fact]
    public void A_totals_row_is_appended_when_a_column_is_totalled()
    {
        var csv = Render(Table(
            new object?[] { "A", 10m, null },
            new object?[] { "B", 15.5m, null }));

        Assert.Contains("Total (2 rows)", csv);
        Assert.Contains("25.5", csv);
    }

    [Fact]
    public void No_totals_row_is_written_for_an_empty_report()
    {
        Assert.DoesNotContain("Total (", Render(Table()));
    }
}

public class ExcelReportExporterTests
{
    private readonly ExcelReportExporter _exporter = new();

    private static ReportTable Table() => new()
    {
        Key = "stock", Title = "Stock levels", Subtitle = "Current position",
        Columns = new[]
        {
            new ReportColumn("Item"),
            new ReportColumn("Quantity", ReportColumnType.Number),
            new ReportColumn("Value", ReportColumnType.Money, Total: true),
            new ReportColumn("Received", ReportColumnType.Date),
            new ReportColumn("Low", ReportColumnType.Boolean)
        },
        Rows = new List<object?[]>
        {
            new object?[] { "Composite A2", 14m, 397.60m, new DateOnly(2026, 6, 1), false },
            new object?[] { "Implant 4.3x10", 6m, 1710m, new DateOnly(2026, 7, 15), true }
        },
        Metrics = new[] { new ReportMetric("Stock value", "£2,107.60", "across 2 lines") }
    };

    [Fact]
    public void The_workbook_opens_and_carries_the_report_title()
    {
        using var stream = new MemoryStream(_exporter.Render(Table()));
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet(1);
        Assert.Equal("Stock levels", sheet.Name);
        Assert.Equal("Stock levels", sheet.Cell(1, 1).GetString());
    }

    [Fact]
    public void Numbers_are_written_as_numbers_not_text()
    {
        using var stream = new MemoryStream(_exporter.Render(Table()));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        var valueCell = sheet.CellsUsed(c => c.Value.IsNumber && Math.Abs(c.GetDouble() - 397.60) < 0.001)
            .FirstOrDefault();

        Assert.NotNull(valueCell);
    }

    [Fact]
    public void Dates_are_written_as_dates()
    {
        using var stream = new MemoryStream(_exporter.Render(Table()));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        var dateCell = sheet.CellsUsed(c => c.Value.IsDateTime).FirstOrDefault();
        Assert.NotNull(dateCell);
        Assert.Equal(new DateTime(2026, 6, 1), dateCell!.GetDateTime());
    }

    [Fact]
    public void The_totals_row_uses_a_subtotal_formula_so_it_survives_filtering()
    {
        using var stream = new MemoryStream(_exporter.Render(Table()));
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        var formula = sheet.CellsUsed(c => c.HasFormula).FirstOrDefault();
        Assert.NotNull(formula);
        Assert.Contains("SUBTOTAL(109", formula!.FormulaA1);
    }

    [Fact]
    public void A_long_title_is_trimmed_to_a_valid_sheet_name()
    {
        var table = Table() with { Title = new string('A', 60) };

        using var stream = new MemoryStream(_exporter.Render(table));
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(31, workbook.Worksheet(1).Name.Length);
    }

    [Fact]
    public void Characters_excel_rejects_are_stripped_from_the_sheet_name()
    {
        var table = Table() with { Title = "Production: 2026/27 [draft]" };

        using var stream = new MemoryStream(_exporter.Render(table));
        using var workbook = new XLWorkbook(stream);

        var name = workbook.Worksheet(1).Name;
        Assert.DoesNotContain(':', name);
        Assert.DoesNotContain('/', name);
        Assert.DoesNotContain('[', name);
    }

    [Fact]
    public void A_report_with_no_columns_still_produces_a_workbook()
    {
        var table = new ReportTable { Title = "Empty" };

        using var stream = new MemoryStream(_exporter.Render(table));
        using var workbook = new XLWorkbook(stream);

        Assert.Equal("Empty", workbook.Worksheet(1).Cell(1, 1).GetString());
    }
}

public class PdfReportExporterTests
{
    private sealed class StubPractice : IPracticeAccessor
    {
        public Domain.Entities.Practice? Current { get; } = new()
        {
            Name = "Meridian Dental Surgery",
            Address = new Domain.Common.Address { Line1 = "42 Harley Mews", City = "London", PostCode = "W1G 8QT" },
            Contact = new Domain.Common.ContactDetails { HomePhone = "020 7946 0812" }
        };

        public void Invalidate() { }
    }

    private readonly PdfReportExporter _exporter;

    public PdfReportExporterTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        _exporter = new PdfReportExporter(new StubPractice());
    }

    private static ReportTable Table(int rows) => new()
    {
        Key = "receivables", Title = "Outstanding balances",
        From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 6, 30),
        Columns = new[]
        {
            new ReportColumn("Patient"),
            new ReportColumn("Balance", ReportColumnType.Money, Total: true)
        },
        Rows = Enumerable.Range(1, rows)
            .Select(i => new object?[] { $"Patient {i}", (decimal)(i * 10) })
            .ToList(),
        Metrics = new[] { new ReportMetric("Total", "£1,000.00") }
    };

    [Fact]
    public void A_pdf_is_produced_with_the_correct_signature()
    {
        var bytes = _exporter.Render(Table(5));

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void An_empty_report_still_renders_rather_than_throwing()
    {
        var bytes = _exporter.Render(Table(0));
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void A_long_report_pages_without_failing()
    {
        // Exercises the repeating header and page breaks.
        var bytes = _exporter.Render(Table(400));
        Assert.True(bytes.Length > 5000);
    }

    [Theory]
    [InlineData(ReportColumnType.Money, "£1,234.50")]
    [InlineData(ReportColumnType.Percent, "1234.5%")]
    [InlineData(ReportColumnType.Integer, "1,235")]
    public void Cells_are_formatted_according_to_the_column_type(ReportColumnType type, string expected)
    {
        Assert.Equal(expected, PdfReportExporter.FormatCell(1234.5m, type));
    }

    [Fact]
    public void A_null_cell_renders_as_an_empty_string()
    {
        Assert.Equal(string.Empty, PdfReportExporter.FormatCell(null, ReportColumnType.Text));
    }

    [Fact]
    public void Enum_values_are_spaced_into_words()
    {
        Assert.Equal("Partially Paid",
            PdfReportExporter.FormatCell(Domain.Enums.InvoiceStatus.PartiallyPaid, ReportColumnType.Text));
    }
}

public class JsonReportExporterTests
{
    private readonly JsonReportExporter _exporter = new();

    [Fact]
    public void Rows_become_objects_keyed_by_column_header()
    {
        var table = new ReportTable
        {
            Key = "patients", Title = "Patients",
            Columns = new[] { new ReportColumn("Name"), new ReportColumn("Balance", ReportColumnType.Money) },
            Rows = new List<object?[]> { new object?[] { "Bertrand", 175m } }
        };

        using var document = System.Text.Json.JsonDocument.Parse(_exporter.Render(table));
        var root = document.RootElement;

        Assert.Equal("patients", root.GetProperty("Key").GetString());
        Assert.Equal(1, root.GetProperty("RowCount").GetInt32());

        var row = root.GetProperty("Rows")[0];
        Assert.Equal("Bertrand", row.GetProperty("Name").GetString());
        Assert.Equal(175m, row.GetProperty("Balance").GetDecimal());
    }

    [Fact]
    public void Enums_and_dates_are_serialised_as_readable_strings()
    {
        var table = new ReportTable
        {
            Title = "Test",
            Columns = new[] { new ReportColumn("Status"), new ReportColumn("Date", ReportColumnType.Date) },
            Rows = new List<object?[]> { new object?[] { Domain.Enums.ClaimStatus.Submitted, new DateOnly(2026, 2, 3) } }
        };

        using var document = System.Text.Json.JsonDocument.Parse(_exporter.Render(table));
        var row = document.RootElement.GetProperty("Rows")[0];

        Assert.Equal("Submitted", row.GetProperty("Status").GetString());
        Assert.Equal("2026-02-03", row.GetProperty("Date").GetString());
    }
}

public class ReportCatalogueDefinitionTests
{
    [Fact]
    public void Every_report_key_is_unique()
    {
        var duplicates = ReportCatalogue.Definitions
            .GroupBy(d => d.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Every_report_is_described_and_categorised()
    {
        Assert.All(ReportCatalogue.Definitions, definition =>
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.Name));
            Assert.False(string.IsNullOrWhiteSpace(definition.Description));
            Assert.False(string.IsNullOrWhiteSpace(definition.Category));
        });
    }

    [Fact]
    public void Reports_are_found_case_insensitively()
    {
        Assert.NotNull(ReportCatalogue.Find("RECEIVABLES"));
        Assert.NotNull(ReportCatalogue.Find("receivables"));
        Assert.Null(ReportCatalogue.Find("no-such-report"));
    }
}
