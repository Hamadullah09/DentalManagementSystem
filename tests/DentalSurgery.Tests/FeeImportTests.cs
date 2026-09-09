using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Tenancy;
using DentalSurgery.Infrastructure.Persistence.Interceptors;
using DentalSurgery.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentalSurgery.Tests;

/// <summary>
/// The importer reads a real price list into a real schedule, so these run
/// against SQLite rather than a substitute: the money round-tripping and the
/// case-insensitive code matching are exactly what would break in production.
/// </summary>
public class FeeImportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestContextFactory _factory;
    private readonly FeeScheduleImporter _importer;

    private readonly Guid _scheduleId = Guid.NewGuid();
    private Guid _examId, _scaleId, _compositeId;

    public FeeImportTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // The auditing interceptor is what stamps the optimistic-concurrency token,
        // so a context without it cannot update its own rows. Register it here too.
        var options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditingInterceptor(new TestUser(), new TestClock(), PlatformTenantContext.Instance))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                    .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        _factory = new TestContextFactory(options);
        _importer = new FeeScheduleImporter(
            _factory, new SystemPermissionGuard(), NullLogger<FeeScheduleImporter>.Instance);

        Seed();
    }

    private void Seed()
    {
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();

        var exam = new ProcedureCode
        {
            Code = "D0150", ShortDescription = "Comprehensive oral evaluation",
            Category = ProcedureCategory.Diagnostic, DefaultFee = 62m, SortOrder = 1
        };
        var scale = new ProcedureCode
        {
            Code = "D1110", ShortDescription = "Scale and polish, adult",
            Category = ProcedureCategory.Preventive, DefaultFee = 78m, SortOrder = 2
        };
        var composite = new ProcedureCode
        {
            Code = "D2392", ShortDescription = "Composite - two surfaces, posterior",
            Category = ProcedureCategory.Restorative, DefaultFee = 155m,
            RequiresTooth = true, SortOrder = 3
        };

        db.ProcedureCodes.AddRange(exam, scale, composite);

        var schedule = new FeeSchedule
        {
            Id = _scheduleId,
            Name = "Private fees 2026",
            ScheduleType = FeeScheduleType.Practice,
            IsDefault = true
        };

        // One code is already priced, so the preview can tell an update from an addition.
        schedule.Items.Add(new FeeScheduleItem
        {
            FeeScheduleId = _scheduleId, ProcedureCodeId = exam.Id, Fee = 60m
        });

        db.FeeSchedules.Add(schedule);
        db.SaveChanges();

        _examId = exam.Id;
        _scaleId = scale.Id;
        _compositeId = composite.Id;
    }

    public void Dispose() => _connection.Dispose();

    // ---------------------------------------------------------------- preview

    [Fact]
    public async Task A_price_list_is_classified_into_additions_updates_and_no_changes()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId, """
            Code,Fee
            D0150,72.00
            D1110,84.50
            D2392,155.00
            """)).Value!;

        Assert.Equal(3, preview.Rows.Count);
        Assert.Equal(3, preview.Valid);
        Assert.Equal(0, preview.Invalid);

        Assert.Equal("Update", preview.Rows.Single(r => r.Code == "D0150").Outcome);
        Assert.Equal("Add", preview.Rows.Single(r => r.Code == "D1110").Outcome);

        // D2392 has no row yet, so it is an addition even though the fee matches the catalogue.
        Assert.Equal("Add", preview.Rows.Single(r => r.Code == "D2392").Outcome);
    }

    [Fact]
    public async Task The_existing_fee_is_reported_alongside_the_new_one()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId, "Code,Fee\nD0150,72.00")).Value!;
        var row = preview.Rows.Single();

        Assert.Equal(60m, row.CurrentFee);
        Assert.Equal(72m, row.Fee);
        Assert.True(row.IsChange);
    }

    [Fact]
    public async Task A_repeated_fee_is_not_counted_as_a_change()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId, "Code,Fee\nD0150,60.00")).Value!;

        Assert.Equal("Unchanged", preview.Rows.Single().Outcome);
        Assert.Equal(0, preview.Changes);
    }

    [Fact]
    public async Task Column_order_and_letter_case_do_not_matter()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId, """
            DESCRIPTION,fee,Coverage,CODE,Allowed
            Scale and polish,84.50,80,d1110,70.00
            """)).Value!;

        var row = preview.Rows.Single();
        Assert.Equal("D1110", row.Code);
        Assert.Equal(84.50m, row.Fee);
        Assert.Equal(70.00m, row.AllowedAmount);
        Assert.Equal(80m, row.CoveragePercent);
    }

    [Fact]
    public async Task Quoted_fields_containing_commas_are_parsed_whole()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId,
            "Code,Description,Fee\nD1110,\"Scale and polish, adult\",84.50")).Value!;

        var row = preview.Rows.Single();
        Assert.Equal("Scale and polish, adult", row.Description);
        Assert.Equal(84.50m, row.Fee);
    }

    [Theory]
    [InlineData("D9999,80.00", "catalogue")]
    [InlineData("D1110,not-a-number", "not a valid fee")]
    [InlineData("D1110,-12.00", "cannot be negative")]
    [InlineData(",80.00", "No procedure code")]
    public async Task Bad_rows_are_rejected_with_the_reason_shown(string row, string expected)
    {
        var preview = (await _importer.PreviewAsync(_scheduleId, $"Code,Fee\n{row}")).Value!;
        var parsed = preview.Rows.Single();

        Assert.False(parsed.IsValid);
        Assert.Contains(expected, parsed.Problem);
        Assert.Equal(1, preview.Invalid);
    }

    [Fact]
    public async Task Coverage_above_a_hundred_percent_is_rejected()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId,
            "Code,Fee,Coverage\nD1110,84.50,120")).Value!;

        Assert.Contains("100%", preview.Rows.Single().Problem);
    }

    [Fact]
    public async Task A_code_repeated_in_the_file_is_taken_once()
    {
        var preview = (await _importer.PreviewAsync(_scheduleId, """
            Code,Fee
            D1110,84.50
            D1110,99.00
            """)).Value!;

        Assert.Equal(1, preview.Valid);
        Assert.Contains("more than once", preview.Rows[1].Problem);
    }

    [Fact]
    public async Task A_file_without_a_code_column_is_refused_outright()
    {
        var result = await _importer.PreviewAsync(_scheduleId, "Price,Notes\n80.00,x");

        Assert.True(result.Failed);
        Assert.Contains("procedure code column", result.ErrorMessage);
    }

    [Fact]
    public async Task A_file_without_a_fee_column_is_refused_outright()
    {
        var result = await _importer.PreviewAsync(_scheduleId, "Code,Notes\nD1110,x");

        Assert.True(result.Failed);
        Assert.Contains("fee column", result.ErrorMessage);
    }

    [Fact]
    public async Task An_empty_file_is_refused()
    {
        Assert.True((await _importer.PreviewAsync(_scheduleId, "   ")).Failed);
        Assert.True((await _importer.PreviewAsync(_scheduleId, "Code,Fee")).Failed);
    }

    [Fact]
    public async Task An_unknown_schedule_is_refused()
    {
        var result = await _importer.PreviewAsync(Guid.NewGuid(), "Code,Fee\nD1110,84.50");

        Assert.True(result.Failed);
        Assert.Contains("not found", result.ErrorMessage);
    }

    // ---------------------------------------------------------------- apply

    [Fact]
    public async Task Applying_updates_existing_rows_and_adds_missing_ones()
    {
        var applied = await _importer.ApplyAsync(_scheduleId, """
            Code,Fee,Allowed,Coverage
            D0150,72.00,58.00,90
            D1110,84.50,70.00,80
            """);

        Assert.Equal(2, applied.Value);

        await using var db = _factory.CreateDbContext();
        var items = await db.FeeScheduleItems
            .Where(i => i.FeeScheduleId == _scheduleId)
            .ToDictionaryAsync(i => i.ProcedureCodeId, i => i);

        Assert.Equal(72m, items[_examId].Fee);
        Assert.Equal(58m, items[_examId].AllowedAmount);
        Assert.Equal(90m, items[_examId].CoveragePercent);

        Assert.Equal(84.50m, items[_scaleId].Fee);
        Assert.Equal(70m, items[_scaleId].AllowedAmount);

        // Untouched codes keep whatever they had; nothing is wiped.
        Assert.False(items.ContainsKey(_compositeId));
    }

    [Fact]
    public async Task Applying_skips_the_rows_the_preview_rejected()
    {
        var applied = await _importer.ApplyAsync(_scheduleId, """
            Code,Fee
            D1110,84.50
            D9999,60.00
            D2392,rubbish
            """);

        Assert.Equal(1, applied.Value);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(2, await db.FeeScheduleItems.CountAsync(i => i.FeeScheduleId == _scheduleId));
    }

    [Fact]
    public async Task Applying_the_same_list_twice_changes_nothing_the_second_time()
    {
        const string csv = "Code,Fee\nD1110,84.50";

        await _importer.ApplyAsync(_scheduleId, csv);
        await _importer.ApplyAsync(_scheduleId, csv);

        await using var db = _factory.CreateDbContext();
        var rows = await db.FeeScheduleItems
            .Where(i => i.FeeScheduleId == _scheduleId && i.ProcedureCodeId == _scaleId)
            .ToListAsync();

        Assert.Single(rows);
        Assert.Equal(84.50m, rows[0].Fee);
    }

    [Fact]
    public async Task Money_survives_the_round_trip_to_two_decimal_places()
    {
        // SQLite stores these as REAL, so a fee like 84.45 is the one that
        // exposes a bad conversion.
        await _importer.ApplyAsync(_scheduleId, "Code,Fee\nD1110,84.45\nD2392,1234.99");

        await using var db = _factory.CreateDbContext();
        var items = await db.FeeScheduleItems
            .Where(i => i.FeeScheduleId == _scheduleId)
            .ToDictionaryAsync(i => i.ProcedureCodeId, i => i.Fee);

        Assert.Equal(84.45m, items[_scaleId]);
        Assert.Equal(1234.99m, items[_compositeId]);
    }

    // ---------------------------------------------------------------- template

    [Fact]
    public async Task The_template_lists_every_active_code_at_its_current_price()
    {
        var template = await _importer.ExportTemplateAsync(_scheduleId);
        var lines = template.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Code,Description,Fee,Allowed,Coverage", lines[0].Trim());
        Assert.Equal(4, lines.Length);                       // header plus three codes

        // D0150 is priced in the schedule at 60; the others fall back to the catalogue fee.
        Assert.Contains(lines, l => l.StartsWith("D0150,") && l.Contains("60.00"));
        Assert.Contains(lines, l => l.StartsWith("D1110,") && l.Contains("78.00"));
    }

    [Fact]
    public async Task The_template_is_a_valid_import_of_itself()
    {
        var template = await _importer.ExportTemplateAsync(_scheduleId);
        var preview = (await _importer.PreviewAsync(_scheduleId, template)).Value!;

        Assert.Equal(0, preview.Invalid);
        Assert.Equal(3, preview.Valid);
    }

    [Fact]
    public async Task Descriptions_containing_commas_are_quoted_in_the_template()
    {
        var template = await _importer.ExportTemplateAsync(_scheduleId);

        Assert.Contains("\"Composite - two surfaces, posterior\"", template);
    }
}

/// <summary>Hands the importer a context over the test's own connection.</summary>
internal sealed class TestContextFactory(DbContextOptions<DentalDbContext> options)
    : IDbContextFactory<DentalDbContext>
{
    public DentalDbContext CreateDbContext() => new(options, PlatformTenantContext.Instance);
}

/// <summary>A signed-in user for the audit trail; the tests do not assert on it.</summary>
internal sealed class TestUser : ICurrentUser
{
    public string? UserId => "test-user";
    public string? UserName => "tests@dentalsurgery.local";
    public string? DisplayName => "Test Harness";
    public Guid? StaffId => null;
    public bool IsAuthenticated => true;
    public IReadOnlyList<string> Roles => new[] { "Administrator" };
    public bool IsInRole(string role) => true;
    public string? IpAddress => "127.0.0.1";
}

internal sealed class TestClock : IDateTimeProvider
{
    public DateTime UtcNow => new(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc);
    public DateTime LocalNow => UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
