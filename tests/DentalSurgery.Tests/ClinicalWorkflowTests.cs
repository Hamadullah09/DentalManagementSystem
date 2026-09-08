using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Clinical;
using DentalSurgery.Application.Scheduling;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Persistence.Interceptors;
using DentalSurgery.Infrastructure.Persistence.Seed;
using DentalSurgery.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentalSurgery.Tests;

/// <summary>
/// A patient's whole journey through the practice, run against the real
/// services, a real database and the real permission guard.
/// <para>
/// Each step is carried out by the role that would really do it, and the guard
/// is resolved from that role's actual grant rather than bypassed. The test
/// therefore asserts two things at once: that the workflow completes, and that
/// it completes without anyone needing access they should not have. A
/// permission removed from the wrong role breaks this test, which is the point.
/// </para>
/// </summary>
public class PatientJourneyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DentalDbContext> _options;
    private readonly ServiceProvider _provider;

    private readonly Guid _providerId;
    private readonly Guid _locationId;
    private readonly Guid _procedureCodeId;

    public PatientJourneyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditingInterceptor(new TestUser(), new TestClock()))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                    .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        var services = new ServiceCollection();
        services.AddSingleton<IDbContextFactory<DentalDbContext>>(new TestContextFactory(_options));
        _provider = services.BuildServiceProvider();

        using var db = new DentalDbContext(_options);
        db.Database.EnsureCreated();

        // The grant is written as role claims, exactly as the running system
        // stores it, so the guard resolves through the real lookup path.
        foreach (var (roleName, permissions) in RolePermissions.Defaults)
        {
            var role = new ApplicationRole(roleName) { Id = Guid.NewGuid().ToString() };
            db.Roles.Add(role);

            foreach (var permission in permissions)
            {
                db.RoleClaims.Add(new IdentityRoleClaim<string>
                {
                    RoleId = role.Id,
                    ClaimType = PermissionCatalogue.ClaimType,
                    ClaimValue = permission
                });
            }
        }

        var practice = new Practice { Name = "Test Dental Practice" };
        db.Practices.Add(practice);

        var location = new Location
        {
            Practice = practice,
            Name = "Main surgery",
            Code = "MAIN",
            IsActive = true
        };
        db.Locations.Add(location);

        // Opening hours, because booking is validated against them. Without
        // these the scheduler correctly refuses every appointment, which is the
        // domain working rather than the test being awkward.
        foreach (var day in new[]
                 {
                     DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                     DayOfWeek.Thursday, DayOfWeek.Friday
                 })
        {
            db.BusinessHours.Add(new BusinessHours
            {
                Location = location,
                DayOfWeek = day,
                IsClosed = false,
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(18, 0, 0)
            });
        }

        var dentist = new Staff
        {
            StaffNumber = "S-9001",
            Name = new PersonName { FirstName = "Test", LastName = "Provider" },
            Role = StaffRole.Dentist,
            IsProvider = true,
            IsActive = true,
            CanPrescribe = true
        };
        db.Staff.Add(dentist);

        var code = new ProcedureCode
        {
            Code = "D0150",
            ShortDescription = "Comprehensive oral evaluation",
            Category = ProcedureCategory.Diagnostic,
            DefaultFee = 62m,
            IsActive = true,
            SortOrder = 1
        };
        db.ProcedureCodes.Add(code);

        foreach (var tooth in ToothReferenceData.Build()) db.Teeth.Add(tooth);

        db.SaveChanges();

        _providerId = dentist.Id;
        _locationId = location.Id;
        _procedureCodeId = code.Id;
    }

    // ------------------------------------------------------------------ wiring

    private IPermissionGuard GuardFor(params string[] roles) =>
        new PermissionGuard(
            new RoleUser(roles),
            new PermissionCatalogue(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<PermissionCatalogue>.Instance),
            NullLogger<PermissionGuard>.Instance);

    private sealed record Actor(
        DentalDbContext Db,
        PatientService Patients,
        AppointmentService Appointments,
        ClinicalService Clinical,
        ProcedureService Procedures,
        BillingService Billing) : IDisposable
    {
        public void Dispose() => Db.Dispose();
    }

    /// <summary>Builds the service set as it would be resolved for one role.</summary>
    private Actor As(params string[] roles)
    {
        var db = new DentalDbContext(_options);
        var guard = GuardFor(roles);
        var sequences = new StubSequences();
        var clock = new TestClock();
        var user = new TestUser();

        var billing = new BillingService(
            db, sequences, user, clock, NullLogger<BillingService>.Instance, guard);

        var inventory = new InventoryService(
            db, sequences, user, clock, NullLogger<InventoryService>.Instance, guard);

        return new Actor(
            db,
            new PatientService(db, sequences, clock, NullLogger<PatientService>.Instance, guard),
            new AppointmentService(db, sequences, clock, NullLogger<AppointmentService>.Instance, guard),
            new ClinicalService(db, sequences, user, clock, NullLogger<ClinicalService>.Instance, guard),
            new ProcedureService(db, billing, inventory, user, clock, NullLogger<ProcedureService>.Instance, guard),
            billing);
    }

    // ------------------------------------------------------------------ journey

    [Fact]
    public async Task A_patient_is_registered_treated_charged_and_paid_by_the_roles_that_do_that_work()
    {
        Guid patientId;
        Guid appointmentId;

        // ---------------------------------------------------------- reception
        using (var reception = As(Roles.Receptionist))
        {
            var created = await reception.Patients.CreateAsync(new Patient
            {
                Name = new PersonName { FirstName = "Journey", LastName = "Patient" },
                DateOfBirth = new DateOnly(1985, 4, 12),
                Gender = Gender.Female,
                Status = PatientStatus.Active,
                Contact = new ContactDetails
                {
                    MobilePhone = "07700 900123",
                    Email = "journey.patient@example.test",
                    PreferredContactMethod = "Email"
                }
            });

            Assert.True(created.Succeeded, string.Join("; ", created.Errors));
            patientId = created.Value!.Id;

            // The patient number comes from the sequence, not from the caller.
            Assert.False(string.IsNullOrWhiteSpace(created.Value.PatientNumber));
        }

        // ---------------------------------------------------------- booking
        using (var reception = As(Roles.Receptionist))
        {
            var start = NextWeekday(3).ToDateTime(new TimeOnly(10, 0));

            var booked = await reception.Appointments.BookAsync(new Appointment
            {
                PatientId = patientId,
                ProviderId = _providerId,
                LocationId = _locationId,
                StartUtc = start,
                EndUtc = start.AddMinutes(30),
                AppointmentType = AppointmentType.RoutineExam,
                Status = AppointmentStatus.Unconfirmed,
                Reason = "Routine examination"
            });

            Assert.True(booked.Succeeded, string.Join("; ", booked.Errors));
            appointmentId = booked.Value!.Id;
        }

        // ---------------------------------------------------------- check-in
        using (var reception = As(Roles.Receptionist))
        {
            Assert.True((await reception.Appointments
                .SetStatusAsync(appointmentId, AppointmentStatus.Confirmed)).Succeeded);

            Assert.True((await reception.Appointments
                .SetStatusAsync(appointmentId, AppointmentStatus.ArrivedWaiting)).Succeeded);
        }

        // ---------------------------------------------------------- treatment
        using (var dentist = As(Roles.Dentist))
        {
            Assert.True((await dentist.Appointments
                .SetStatusAsync(appointmentId, AppointmentStatus.InTreatment)).Succeeded);

            var chart = await dentist.Clinical.GetChartAsync(patientId);
            Assert.NotNull(chart);

            var note = await dentist.Clinical.SaveNoteAsync(new ClinicalNote
            {
                PatientId = patientId,
                AppointmentId = appointmentId,
                ProviderId = _providerId,
                NoteType = ClinicalNoteType.Soap,
                Subjective = "Attends for routine examination. No complaints.",
                Objective = "Soft tissues clear. No caries detected.",
                Assessment = "Dentally fit.",
                Plan = "Routine recall in six months."
            }, sign: true);

            Assert.True(note.Succeeded, string.Join("; ", note.Errors));
            Assert.True(note.Value!.IsSigned);

            // A signed note is a finalised record: amended afterwards, not edited.
            Assert.False(note.Value.IsEditable);

            var completed = await dentist.Procedures.CompleteAsync(new CompleteProcedureRequest
            {
                PatientId = patientId,
                ProcedureCodeId = _procedureCodeId,
                ProviderId = _providerId,
                AppointmentId = appointmentId,
                LocationId = _locationId
            });

            Assert.True(completed.Succeeded, string.Join("; ", completed.Errors));
        }

        // ---------------------------------------------------------- money
        using (var office = As(Roles.PracticeManager))
        {
            var account = await office.Billing.GetAccountAsync(patientId);

            // The charge follows from the treatment recorded, rather than being
            // entered separately, so the ledger cannot disagree with the record.
            Assert.True(account.TotalCharged > 0m,
                "Completing a procedure should have produced a charge.");

            var payment = await office.Billing.TakePaymentAsync(
                patientId, account.Balance, PaymentMethod.DebitCard, reference: "journey-test");

            Assert.True(payment.Succeeded, string.Join("; ", payment.Errors));

            var settled = await office.Billing.GetAccountAsync(patientId);
            Assert.Equal(0m, settled.Balance);
        }

        // ---------------------------------------------------------- close out
        using (var reception = As(Roles.Receptionist))
        {
            Assert.True((await reception.Appointments
                .SetStatusAsync(appointmentId, AppointmentStatus.Completed)).Succeeded);

            Assert.True((await reception.Appointments
                .SetStatusAsync(appointmentId, AppointmentStatus.CheckedOut)).Succeeded);
        }

        // ---------------------------------------------------------- audited
        using (var db = new DentalDbContext(_options))
        {
            var trail = await db.AuditLogs.AsNoTracking()
                .Where(a => a.EntityId == patientId.ToString()
                         || a.EntityId == appointmentId.ToString())
                .ToListAsync();

            Assert.NotEmpty(trail);

            // Every entry names an operator. A row without one answers none of
            // the questions an audit trail exists to answer.
            Assert.All(trail, entry => Assert.False(string.IsNullOrWhiteSpace(entry.UserName)));
        }
    }

    // ------------------------------------------------------------------ refusals

    [Fact]
    public async Task Reception_cannot_write_the_clinical_note_the_dentist_writes()
    {
        var patientId = await RegisterAsync();

        using var reception = As(Roles.Receptionist);

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => reception.Clinical.SaveNoteAsync(new ClinicalNote
            {
                PatientId = patientId,
                Body = "Reception should not be able to write this."
            }, sign: false));

        Assert.Equal(Permissions.ClinicalRecordsCreate, refusal.Permission);
    }

    [Fact]
    public async Task Signing_a_note_needs_more_than_writing_one()
    {
        var patientId = await RegisterAsync();

        // A role that may draft but not sign is refused at the signature, not at
        // the keyboard — which is the distinction the permission exists to draw.
        using var nurse = As(Roles.Nurse);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => nurse.Clinical.SaveNoteAsync(new ClinicalNote
            {
                PatientId = patientId,
                Body = "Chairside observation."
            }, sign: true));
    }

    [Fact]
    public async Task A_hygienist_works_the_periodontal_record_and_cannot_prescribe()
    {
        var patientId = await RegisterAsync();

        using var hygienist = As(Roles.Hygienist);

        Assert.NotNull(await hygienist.Clinical.GetPerioChartsAsync(patientId));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => hygienist.Clinical.IssuePrescriptionAsync(
                new Prescription { PatientId = patientId }, acknowledgeWarnings: true));
    }

    [Fact]
    public async Task A_practice_manager_moves_money_and_cannot_read_the_notes()
    {
        var patientId = await RegisterAsync();

        using var office = As(Roles.PracticeManager);

        // The ledger is theirs.
        Assert.NotNull(await office.Billing.GetAccountAsync(patientId));

        // The clinical record is not.
        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => office.Clinical.GetNotesAsync(patientId));

        Assert.Equal(Permissions.ClinicalRecordsView, refusal.Permission);
    }

    [Fact]
    public async Task Cancelling_an_appointment_needs_a_different_permission_from_checking_one_in()
    {
        var patientId = await RegisterAsync();
        Guid appointmentId;

        using (var reception = As(Roles.Receptionist))
        {
            var start = NextWeekday(4).ToDateTime(new TimeOnly(9, 0));

            var booked = await reception.Appointments.BookAsync(new Appointment
            {
                PatientId = patientId,
                ProviderId = _providerId,
                LocationId = _locationId,
                StartUtc = start,
                EndUtc = start.AddMinutes(30),
                AppointmentType = AppointmentType.RoutineExam
            });

            Assert.True(booked.Succeeded, string.Join("; ", booked.Errors));
            appointmentId = booked.Value!.Id;
        }

        // A nurse may move a patient through the visit …
        using (var nurse = As(Roles.Nurse))
        {
            Assert.True((await nurse.Appointments
                .SetStatusAsync(appointmentId, AppointmentStatus.ArrivedWaiting)).Succeeded);

            // … and may not record a non-attendance, which has a financial and
            // recall consequence.
            var refusal = await Assert.ThrowsAsync<ForbiddenException>(
                () => nurse.Appointments.SetStatusAsync(appointmentId, AppointmentStatus.NoShow));

            Assert.Equal(Permissions.AppointmentsCancel, refusal.Permission);
        }
    }

    /// <summary>
    /// A weekday at least <paramref name="minimumDaysAhead"/> from now. The
    /// clinic is closed at weekends, so a fixed offset from "today" would make
    /// the test pass or fail depending on the day it was run.
    /// </summary>
    private static DateOnly NextWeekday(int minimumDaysAhead)
    {
        var date = new TestClock().Today.AddDays(minimumDaysAhead);

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);

        return date;
    }

    private async Task<Guid> RegisterAsync()
    {
        using var reception = As(Roles.Receptionist);

        // Contact details are required by the domain, which is why they are here
        // rather than left out for brevity.
        var created = await reception.Patients.CreateAsync(new Patient
        {
            Name = new PersonName { FirstName = "Case", LastName = "Subject" },
            DateOfBirth = new DateOnly(1979, 2, 2),
            Status = PatientStatus.Active,
            Contact = new ContactDetails
            {
                MobilePhone = "07700 900456",
                PreferredContactMethod = "Phone"
            }
        });

        Assert.True(created.Succeeded, string.Join("; ", created.Errors));
        return created.Value!.Id;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    /// <summary>A principal holding exactly the roles a test names.</summary>
    private sealed class RoleUser(string[] roles) : ICurrentUser
    {
        public string? UserId => "journey-test";
        public string? UserName => "journey@dentalsurgery.local";
        public string? DisplayName => "Journey Test";
        public Guid? StaffId => null;
        public bool IsAuthenticated => true;
        public IReadOnlyList<string> Roles => roles;
        public bool IsInRole(string role) => roles.Contains(role);
        public string? IpAddress => "127.0.0.1";
    }

    private sealed class StubSequences : INumberSequenceService
    {
        private int _next = 1;
        public Task<string> NextAsync(string sequenceName, CancellationToken ct = default) =>
            Task.FromResult($"{sequenceName}-{_next++:000000}");
    }
}
