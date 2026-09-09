using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Persistence.Interceptors;
using DentalSurgery.Infrastructure.Services;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace DentalSurgery.Tests;

/// <summary>
/// The data-protection duties: hand a patient their record, refuse to erase it
/// while it must be kept, and remove what the retention policy says to remove.
/// </summary>
public class DataProtectionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DentalDbContext> _options;
    private readonly TenantContext _tenancy = TestTenancy.Bound();
    private readonly TestClock _clock = new();

    private Guid _adultId;
    private Guid _childId;
    private Guid _debtorId;

    public DataProtectionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new TenantGuardInterceptor(_tenancy, NullLogger<TenantGuardInterceptor>.Instance))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                    .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        Seed();
    }

    private DentalDbContext NewContext() => new(_options, _tenancy);

    private DataProtectionService ServiceFor(params string[] permissions) =>
        new(NewContext(), _clock, new StubGuard(permissions), NullLogger<DataProtectionService>.Instance);

    public void Dispose() => _connection.Dispose();

    private void Seed()
    {
        using var db = NewContext();
        db.Database.EnsureCreated();

        // Treated long ago, adult: retention has expired.
        var adult = NewPatient("P-ADULT", "Ada", new DateOnly(1960, 3, 1));

        // Treated long ago but still young: the 25th-birthday rule still holds it.
        var child = NewPatient("P-CHILD", "Cass", new DateOnly(2015, 6, 1));

        // Retention expired, but the account is not settled.
        var debtor = NewPatient("P-DEBT", "Des", new DateOnly(1955, 1, 1));

        db.Patients.AddRange(adult, child, debtor);

        db.AppSettings.Add(new AppSetting
        {
            Key = "Security.AuditRetentionYears",
            Value = "7",
            Category = "Security",
            DataType = "int"
        });

        db.SaveChanges();

        _adultId = adult.Id;
        _childId = child.Id;
        _debtorId = debtor.Id;

        // A note fifteen years old, so the ten-year clock has run out.
        var longAgo = _clock.UtcNow.AddYears(-15);

        foreach (var id in new[] { _adultId, _childId, _debtorId })
        {
            db.ClinicalNotes.Add(new ClinicalNote
            {
                PatientId = id,
                CreatedAtUtc = longAgo,
                Subjective = "Historic visit."
            });
        }

        db.LedgerEntries.Add(new LedgerEntry
        {
            PatientId = _debtorId,
            EntryDate = DateOnly.FromDateTime(longAgo),
            Description = "Unpaid treatment",
            Debit = 250m
        });

        db.SaveChanges();
    }

    private static Patient NewPatient(string number, string first, DateOnly dob) => new()
    {
        PatientNumber = number,
        Name = new PersonName { FirstName = first, LastName = "Test" },
        DateOfBirth = dob,
        Contact = new ContactDetails { Email = $"{first}@example.test", MobilePhone = "07700900000" },
        Address = new Address { Line1 = "1 Test Street", PostCode = "T1 1ST" },
        NhsNumber = "123 456 7890"
    };

    // ---------------------------------------------------------------- access

    [Fact]
    public async Task A_subject_access_export_carries_the_whole_record()
    {
        var export = await ServiceFor(Permissions.DataProtectionExport).ExportPatientAsync(_adultId);

        using var document = JsonDocument.Parse(export.Json);
        var sections = document.RootElement.GetProperty("sections");

        // Named explicitly, because the point of the export is that nothing is
        // quietly left out of what the practice hands over.
        foreach (var section in new[]
                 {
                     "patient", "medicalConditions", "allergies", "medications", "dentalChart",
                     "periodontalCharts", "clinicalNotes", "prescriptions", "radiographs",
                     "treatmentPlans", "procedures", "appointments", "invoices", "payments",
                     "ledger", "documents", "communications"
                 })
        {
            Assert.True(sections.TryGetProperty(section, out _), $"The export is missing '{section}'.");
        }
    }

    [Fact]
    public async Task An_export_needs_the_export_permission()
    {
        var service = ServiceFor(Permissions.PatientsView);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ExportPatientAsync(_adultId));
    }

    [Fact]
    public async Task The_export_is_named_after_the_patient_it_describes()
    {
        var export = await ServiceFor(Permissions.DataProtectionExport).ExportPatientAsync(_adultId);

        Assert.Equal("subject-access-P-ADULT.json", export.FileName);
    }

    // ---------------------------------------------------------------- retention

    [Fact]
    public async Task A_record_past_its_retention_period_may_be_erased()
    {
        var assessment = await ServiceFor(Permissions.DataProtectionReview).AssessErasureAsync(_adultId);

        Assert.False(assessment.Blocked);
    }

    [Fact]
    public async Task A_childs_record_is_held_until_they_are_twenty_five()
    {
        // Treated fifteen years ago, so the ten-year clock has expired - but the
        // patient is still a child, and that rule is the later of the two.
        var assessment = await ServiceFor(Permissions.DataProtectionReview).AssessErasureAsync(_childId);

        Assert.True(assessment.Blocked);
        Assert.Contains(assessment.Reasons, r => r.Contains("25th birthday"));
        Assert.Equal(2040, assessment.RetentionClearsOn.Year);
    }

    [Fact]
    public async Task An_unsettled_account_holds_the_record()
    {
        var assessment = await ServiceFor(Permissions.DataProtectionReview).AssessErasureAsync(_debtorId);

        Assert.True(assessment.Blocked);
        Assert.Contains(assessment.Reasons, r => r.Contains("not settled"));
        Assert.Equal(250m, assessment.OutstandingBalance);
    }

    [Fact]
    public async Task Erasure_is_refused_while_the_record_must_be_kept()
    {
        var service = ServiceFor(Permissions.DataProtectionErase, Permissions.DataProtectionReview);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ErasePatientAsync(_childId, "Patient asked"));

        Assert.Contains("cannot be erased yet", refusal.Message);
    }

    // ---------------------------------------------------------------- erasure

    [Fact]
    public async Task Erasure_removes_the_person_and_keeps_the_treatment()
    {
        var service = ServiceFor(Permissions.DataProtectionErase, Permissions.DataProtectionReview);

        await service.ErasePatientAsync(_adultId, "Subject request, retention expired");

        using var db = NewContext();
        var patient = db.Patients.Single(p => p.Id == _adultId);

        Assert.True(patient.IsErased);
        Assert.Equal("Erased", patient.Name.FirstName);
        Assert.Null(patient.Contact.Email);
        Assert.Null(patient.Contact.MobilePhone);
        Assert.Null(patient.NhsNumber);
        Assert.Null(patient.Address.Line1);

        // The clinical record survives: what happened is a fact about the
        // practice too, and deleting it would falsify the audit trail.
        Assert.NotEmpty(db.ClinicalNotes.Where(n => n.PatientId == _adultId).ToList());
    }

    [Fact]
    public async Task Erasure_keeps_the_year_of_birth_and_nothing_finer()
    {
        var service = ServiceFor(Permissions.DataProtectionErase, Permissions.DataProtectionReview);

        await service.ErasePatientAsync(_adultId, "Subject request");

        using var db = NewContext();
        var patient = db.Patients.Single(p => p.Id == _adultId);

        // Age at treatment stays clinically meaningful; a year alone identifies
        // nobody.
        Assert.Equal(new DateOnly(1960, 1, 1), patient.DateOfBirth);
    }

    [Fact]
    public async Task An_erasure_must_say_why()
    {
        var service = ServiceFor(Permissions.DataProtectionErase, Permissions.DataProtectionReview);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ErasePatientAsync(_adultId, "   "));
    }

    [Fact]
    public async Task Erasure_needs_more_than_permission_to_review()
    {
        var service = ServiceFor(Permissions.DataProtectionReview);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ErasePatientAsync(_adultId, "Subject request"));
    }

    [Fact]
    public async Task Retention_may_be_overridden_deliberately()
    {
        // There are lawful reasons to erase early — a record created in error,
        // for instance — so the override exists, is explicit, and is logged.
        var service = ServiceFor(Permissions.DataProtectionErase, Permissions.DataProtectionReview);

        var outcome = await service.ErasePatientAsync(
            _childId, "Record created in error", overrideRetention: true);

        Assert.Equal("P-CHILD", outcome.PatientNumber);
    }

    // ---------------------------------------------------------------- audit purge

    [Fact]
    public async Task The_configured_retention_period_is_the_one_that_is_used()
    {
        // Seeded as 7 rather than the 10-year default, so a value that was being
        // ignored would show up here.
        var years = await ServiceFor(Permissions.DataProtectionReview).ConfiguredRetentionYearsAsync();

        Assert.Equal(7, years);
    }

    [Fact]
    public async Task Audit_rows_past_the_retention_period_are_removed()
    {
        using (var db = NewContext())
        {
            db.AuditLogs.AddRange(
                new AuditLog { TenantId = TestTenancy.TenantId, TimestampUtc = _clock.UtcNow.AddYears(-8), EntityName = "Patient" },
                new AuditLog { TenantId = TestTenancy.TenantId, TimestampUtc = _clock.UtcNow.AddYears(-1), EntityName = "Patient" });
            db.SaveChanges();
        }

        var removed = await ServiceFor(Permissions.DataProtectionReview).PurgeExpiredAuditAsync();

        Assert.Equal(1, removed);

        using var check = NewContext();
        Assert.Single(check.AuditLogs.ToList());
    }

    /// <summary>Grants exactly the permissions named, and nothing else.</summary>
    private sealed class StubGuard(params string[] permissions) : IPermissionGuard
    {
        private readonly HashSet<string> _held = new(permissions, StringComparer.Ordinal);

        public Task<bool> HasAsync(string permission, CancellationToken ct = default) =>
            Task.FromResult(_held.Contains(permission));

        public Task<bool> HasAllAsync(IEnumerable<string> permissions, CancellationToken ct = default) =>
            Task.FromResult(permissions.All(_held.Contains));

        public Task<bool> HasAnyAsync(IEnumerable<string> permissions, CancellationToken ct = default) =>
            Task.FromResult(permissions.Any(_held.Contains));

        public Task DemandAsync(string permission, CancellationToken ct = default) =>
            _held.Contains(permission)
                ? Task.CompletedTask
                : throw new ForbiddenException(permission);

        public Task<IReadOnlySet<string>> CurrentAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<string>>(_held);
    }
}
