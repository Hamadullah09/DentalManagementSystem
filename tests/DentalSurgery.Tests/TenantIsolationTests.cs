using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Persistence.Interceptors;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentalSurgery.Tests;

/// <summary>
/// The tests that decide whether this system may be run as SaaS at all.
/// <para>
/// Everything here asserts one thing from a different angle: work done while
/// signed in to one practice must not read, change or destroy another
/// practice's records. They run against real SQLite rather than the in-memory
/// provider, because the in-memory provider does not apply relational
/// behaviour faithfully and this is precisely the code that must not be tested
/// against an approximation.
/// </para>
/// </summary>
public class TenantIsolationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DentalDbContext> _options;
    private readonly TenantContext _tenant = new(NullLogger<TenantContext>.Instance);

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public TenantIsolationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new TenantGuardInterceptor(_tenant, NullLogger<TenantGuardInterceptor>.Instance))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                    .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        using (_tenant.EnterPlatformScope("test setup"))
        using (var db = NewContext())
        {
            db.Database.EnsureCreated();

            db.Tenants.AddRange(
                new Tenant { Id = TenantA, Name = "Practice A", Slug = "a" },
                new Tenant { Id = TenantB, Name = "Practice B", Slug = "b" });

            db.Patients.AddRange(
                NewPatient(TenantA, "A-1", "Alice"),
                NewPatient(TenantA, "A-2", "Amara"),
                NewPatient(TenantB, "B-1", "Bruno"));

            db.SaveChanges();
        }
    }

    private DentalDbContext NewContext() => new(_options, _tenant);

    private static Patient NewPatient(Guid tenantId, string number, string first) => new()
    {
        TenantId = tenantId,
        PatientNumber = number,
        Name = new PersonName { FirstName = first, LastName = "Test" },
        DateOfBirth = new DateOnly(1980, 1, 1)
    };

    public void Dispose() => _connection.Dispose();

    // ------------------------------------------------------------------ reads

    [Fact]
    public void A_tenant_sees_only_its_own_patients()
    {
        using var _ = _tenant.EnterTenant(TenantA);
        using var db = NewContext();

        var numbers = db.Patients.Select(p => p.PatientNumber).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "A-1", "A-2" }, numbers);
    }

    [Fact]
    public void The_other_tenant_sees_only_its_own()
    {
        using var _ = _tenant.EnterTenant(TenantB);
        using var db = NewContext();

        Assert.Equal(new[] { "B-1" }, db.Patients.Select(p => p.PatientNumber).ToList());
    }

    [Fact]
    public void A_record_cannot_be_fetched_by_id_across_tenants()
    {
        // Guessing an identifier is the obvious attack, and the one a filter
        // applied only to list screens would miss.
        Guid foreignId;
        using (_tenant.EnterTenant(TenantB))
        using (var db = NewContext())
            foreignId = db.Patients.Single().Id;

        using var __ = _tenant.EnterTenant(TenantA);
        using var context = NewContext();

        Assert.Null(context.Patients.FirstOrDefault(p => p.Id == foreignId));
    }

    [Fact]
    public void Counts_and_aggregates_do_not_leak_across_tenants()
    {
        // A count is a small leak but still a leak: it tells one practice how
        // many patients another has.
        using var _ = _tenant.EnterTenant(TenantA);
        using var db = NewContext();

        Assert.Equal(2, db.Patients.Count());
        Assert.False(db.Patients.Any(p => p.PatientNumber == "B-1"));
    }

    [Fact]
    public void A_scope_with_no_tenant_sees_nothing()
    {
        // The default posture. An unauthenticated request, or one whose tenant
        // failed to resolve, must not fall back to seeing everything.
        using var db = NewContext();

        Assert.Empty(db.Patients.ToList());
    }

    [Fact]
    public void A_platform_scope_sees_every_tenant()
    {
        using var _ = _tenant.EnterPlatformScope("test");
        using var db = NewContext();

        Assert.Equal(3, db.Patients.Count());
    }

    // ------------------------------------------------------------------ writes

    [Fact]
    public void A_new_record_is_stamped_with_the_current_tenant()
    {
        using var _ = _tenant.EnterTenant(TenantB);
        using var db = NewContext();

        // Deliberately left unset: application code should never have to
        // remember, and forgetting must not produce an orphan row.
        db.Patients.Add(new Patient
        {
            PatientNumber = "B-2",
            Name = new PersonName { FirstName = "Bianca", LastName = "Test" },
            DateOfBirth = new DateOnly(1990, 5, 5)
        });
        db.SaveChanges();

        var saved = db.Patients.Single(p => p.PatientNumber == "B-2");
        Assert.Equal(TenantB, saved.TenantId);
    }

    [Fact]
    public void A_record_cannot_be_created_inside_another_tenant()
    {
        using var _ = _tenant.EnterTenant(TenantA);
        using var db = NewContext();

        db.Patients.Add(NewPatient(TenantB, "SMUGGLED", "Mallory"));

        Assert.Throws<TenantIsolationException>(() => db.SaveChanges());
    }

    [Fact]
    public void A_write_with_no_tenant_in_scope_is_refused()
    {
        using var db = NewContext();

        db.Patients.Add(new Patient
        {
            PatientNumber = "ORPHAN",
            Name = new PersonName { FirstName = "No", LastName = "Tenant" },
            DateOfBirth = new DateOnly(1990, 1, 1)
        });

        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public void Another_tenants_record_cannot_be_updated()
    {
        // The realistic leak: the entity arrives from somewhere the filter did
        // not cover, and the update then looks perfectly ordinary.
        Patient foreign;
        using (_tenant.EnterPlatformScope("fetch across tenants"))
        using (var db = NewContext())
            foreign = db.Patients.Single(p => p.PatientNumber == "B-1");

        using var _ = _tenant.EnterTenant(TenantA);
        using var context = NewContext();

        context.Attach(foreign);
        foreign.Name.FirstName = "Tampered";
        context.Entry(foreign).State = EntityState.Modified;

        Assert.Throws<TenantIsolationException>(() => context.SaveChanges());
    }

    [Fact]
    public void Another_tenants_record_cannot_be_deleted()
    {
        Patient foreign;
        using (_tenant.EnterPlatformScope("fetch across tenants"))
        using (var db = NewContext())
            foreign = db.Patients.Single(p => p.PatientNumber == "B-1");

        using var _ = _tenant.EnterTenant(TenantA);
        using var context = NewContext();

        context.Remove(foreign);

        Assert.Throws<TenantIsolationException>(() => context.SaveChanges());
    }

    [Fact]
    public void A_record_cannot_be_moved_between_tenants()
    {
        using var _ = _tenant.EnterTenant(TenantA);
        using var db = NewContext();

        var mine = db.Patients.First(p => p.PatientNumber == "A-1");
        mine.TenantId = TenantB;

        Assert.Throws<TenantIsolationException>(() => db.SaveChanges());
    }

    // ------------------------------------------------------------------ model

    [Fact]
    public void Every_entity_is_either_tenant_scoped_or_explicitly_global()
    {
        // The model guard runs when the context is built, so reaching this line
        // at all means no entity was left unclassified. Asserting it explicitly
        // keeps the reason visible when it one day fails.
        using var _ = _tenant.EnterPlatformScope("model inspection");
        using var db = NewContext();

        var unclassified = db.Model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .Select(e => e.ClrType)
            .Where(t => !typeof(ITenantScoped).IsAssignableFrom(t))
            .Where(t => !typeof(IGlobalEntity).IsAssignableFrom(t))
            .Where(t => t.Namespace?.StartsWith("DentalSurgery", StringComparison.Ordinal) == true)
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(unclassified);
    }

    [Fact]
    public void Clinical_reference_data_is_shared_rather_than_duplicated()
    {
        // Tooth anatomy and the code catalogue are the same for everyone;
        // copying them per tenant would be waste, and worse, would let them
        // drift apart.
        Assert.True(typeof(IGlobalEntity).IsAssignableFrom(typeof(Tooth)));
        Assert.True(typeof(IGlobalEntity).IsAssignableFrom(typeof(ProcedureCode)));
        Assert.True(typeof(IGlobalEntity).IsAssignableFrom(typeof(Allergen)));
        Assert.True(typeof(IGlobalEntity).IsAssignableFrom(typeof(MedicalCondition)));
        Assert.True(typeof(IGlobalEntity).IsAssignableFrom(typeof(Medication)));
    }

    [Theory]
    [InlineData(typeof(Patient))]
    [InlineData(typeof(ClinicalNote))]
    [InlineData(typeof(Invoice))]
    [InlineData(typeof(Appointment))]
    [InlineData(typeof(AuditLog))]
    [InlineData(typeof(NumberSequence))]
    [InlineData(typeof(FeeSchedule))]
    [InlineData(typeof(MessageTemplate))]
    public void Records_that_belong_to_a_practice_are_scoped(Type type)
    {
        // NumberSequence in particular: shared sequences would collide, and one
        // practice could infer another's patient volume from the gaps.
        Assert.True(typeof(ITenantScoped).IsAssignableFrom(type),
            $"{type.Name} holds practice data and must be tenant-scoped.");
    }
}
