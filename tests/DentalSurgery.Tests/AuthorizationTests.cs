using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Tenancy;
using DentalSurgery.Infrastructure.Persistence.Interceptors;
using DentalSurgery.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace DentalSurgery.Tests;

// ---------------------------------------------------------------------------
// The permission matrix itself.
//
// These are assertions about policy rather than about code paths, and they are
// deliberately written as "this role must not hold this" rather than counting
// permissions. A count passes when the wrong permission is swapped for the
// right one; naming the permission does not.
// ---------------------------------------------------------------------------

public class RolePermissionMatrixTests
{
    [Fact]
    public void Every_role_named_in_the_defaults_is_a_role_the_application_defines()
    {
        foreach (var role in RolePermissions.Defaults.Keys)
            Assert.Contains(role, Roles.All);
    }

    [Fact]
    public void Every_role_the_application_defines_has_a_default_grant()
    {
        foreach (var role in Roles.All)
            Assert.True(RolePermissions.Defaults.ContainsKey(role), $"{role} has no default permission grant.");
    }

    [Fact]
    public void Every_granted_permission_is_one_this_build_defines()
    {
        foreach (var (role, granted) in RolePermissions.Defaults)
        foreach (var permission in granted)
        {
            Assert.True(Permissions.IsDefined(permission),
                $"{role} is granted '{permission}', which is not a defined permission.");
        }
    }

    [Fact]
    public void Every_defined_permission_has_a_description_for_the_admin_screen()
    {
        foreach (var permission in Permissions.All)
            Assert.True(Permissions.Descriptions.ContainsKey(permission), $"{permission} has no description.");
    }

    [Fact]
    public void Every_permission_module_appears_in_the_display_order()
    {
        foreach (var permission in Permissions.All)
        {
            var module = Permissions.ModuleOf(permission);
            Assert.Contains(module, Permissions.ModuleOrder);
        }
    }

    [Fact]
    public void The_administrator_holds_every_permission()
    {
        var admin = RolePermissions.For(Roles.Administrator);

        foreach (var permission in Permissions.All)
            Assert.Contains(permission, admin);
    }

    // ---------------------------------------------------------------- reception

    [Theory]
    [InlineData(Permissions.ClinicalRecordsView)]
    [InlineData(Permissions.ClinicalRecordsCreate)]
    [InlineData(Permissions.MedicalHistoryView)]
    [InlineData(Permissions.DentalChartView)]
    [InlineData(Permissions.DentalChartEdit)]
    [InlineData(Permissions.PeriodontalView)]
    [InlineData(Permissions.PrescriptionsCreate)]
    [InlineData(Permissions.SurgeryView)]
    [InlineData(Permissions.BillingRefund)]
    [InlineData(Permissions.RolesEdit)]
    public void Reception_cannot_reach_the_clinical_record_or_change_money(string permission)
    {
        Assert.DoesNotContain(permission, RolePermissions.For(Roles.Receptionist));
    }

    [Theory]
    [InlineData(Permissions.PatientsView)]
    [InlineData(Permissions.PatientsCreate)]
    [InlineData(Permissions.AppointmentsCreate)]
    [InlineData(Permissions.AppointmentsCancel)]
    [InlineData(Permissions.WaitingRoomManage)]
    [InlineData(Permissions.BillingCreate)]
    public void Reception_can_do_its_own_job(string permission)
    {
        Assert.Contains(permission, RolePermissions.For(Roles.Receptionist));
    }

    // ---------------------------------------------------------------- hygiene

    [Theory]
    [InlineData(Permissions.PeriodontalEdit)]
    [InlineData(Permissions.DentalChartEdit)]
    [InlineData(Permissions.ClinicalRecordsSign)]
    [InlineData(Permissions.SterilizationRecordCycle)]
    public void A_hygienist_can_work_the_preventive_record(string permission)
    {
        Assert.Contains(permission, RolePermissions.For(Roles.Hygienist));
    }

    [Theory]
    [InlineData(Permissions.PrescriptionsCreate)]
    [InlineData(Permissions.SurgeryCreate)]
    [InlineData(Permissions.ImplantsRecord)]
    [InlineData(Permissions.BillingRefund)]
    [InlineData(Permissions.ReportsFinancial)]
    public void A_hygienist_cannot_prescribe_operate_or_move_money(string permission)
    {
        Assert.DoesNotContain(permission, RolePermissions.For(Roles.Hygienist));
    }

    // ---------------------------------------------------------------- surgery

    [Fact]
    public void Only_the_oral_surgeon_and_the_administrator_may_register_an_implant()
    {
        var permitted = Roles.All
            .Where(r => RolePermissions.For(r).Contains(Permissions.ImplantsRecord))
            .ToArray();

        Assert.Equal([Roles.Administrator, Roles.OralSurgeon], permitted.OrderBy(r => r).ToArray());
    }

    [Fact]
    public void An_oral_surgeon_holds_everything_a_dentist_holds()
    {
        var dentist = RolePermissions.For(Roles.Dentist);
        var surgeon = RolePermissions.For(Roles.OralSurgeon);

        foreach (var permission in dentist)
            Assert.Contains(permission, surgeon);
    }

    // ---------------------------------------------------------------- management

    [Theory]
    [InlineData(Permissions.ClinicalRecordsView)]
    [InlineData(Permissions.ClinicalRecordsEdit)]
    [InlineData(Permissions.DentalChartEdit)]
    [InlineData(Permissions.MedicalHistoryView)]
    [InlineData(Permissions.PeriodontalView)]
    [InlineData(Permissions.PrescriptionsCreate)]
    [InlineData(Permissions.SurgeryView)]
    public void A_practice_manager_holds_no_clinical_access(string permission)
    {
        Assert.DoesNotContain(permission, RolePermissions.For(Roles.PracticeManager));
    }

    [Theory]
    [InlineData(Permissions.BillingRefund)]
    [InlineData(Permissions.InsuranceSubmitClaim)]
    [InlineData(Permissions.ReportsFinancial)]
    [InlineData(Permissions.InventoryAdjust)]
    [InlineData(Permissions.UsersDisable)]
    public void A_practice_manager_runs_the_business(string permission)
    {
        Assert.Contains(permission, RolePermissions.For(Roles.PracticeManager));
    }

    [Fact]
    public void Only_the_administrator_may_change_what_a_role_is_allowed_to_do()
    {
        foreach (var role in Roles.All.Where(r => r != Roles.Administrator))
            Assert.DoesNotContain(Permissions.RolesEdit, RolePermissions.For(role));
    }

    [Fact]
    public void Only_the_administrator_may_read_the_audit_trail()
    {
        foreach (var role in Roles.All.Where(r => r != Roles.Administrator))
            Assert.DoesNotContain(Permissions.AuditView, RolePermissions.For(role));
    }

    [Fact]
    public void A_read_only_account_holds_no_permission_that_changes_anything()
    {
        var mutating = new[]
        {
            "Create", "Edit", "Delete", "Sign", "Approve", "Record", "Adjust",
            "Refund", "SubmitClaim", "Manage", "Upload", "Send", "Disable", "RecordCycle"
        };

        foreach (var permission in RolePermissions.For(Roles.ReadOnly))
        {
            var action = Permissions.ActionOf(permission);
            Assert.DoesNotContain(action, mutating);
        }
    }

    [Fact]
    public void The_six_primary_roles_are_the_ones_the_practice_is_organised_around()
    {
        Assert.Equal(
            [Roles.Administrator, Roles.Dentist, Roles.Hygienist,
             Roles.OralSurgeon, Roles.PracticeManager, Roles.Receptionist],
            Roles.Primary.OrderBy(r => r, StringComparer.Ordinal).ToArray());
    }
}

// ---------------------------------------------------------------------------
// Enforcement, not policy: does the service layer actually refuse?
//
// These call the real services with a real database and a guard built from a
// real role grant. A page that forgot its [Authorize] attribute, or a caller
// that arrives by some route nobody anticipated, still has to get past these.
// ---------------------------------------------------------------------------

public class ServiceAuthorizationTests : IDisposable
{
    private readonly TenantContext _tenancy = TestTenancy.Bound();
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DentalDbContext> _options;
    private readonly ServiceProvider _provider;

    private Guid _patientId;

    public ServiceAuthorizationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                // The guard stamps TenantId on insert. Without it the fixture
                // would write rows belonging to no tenant, which the filters
                // then hide - so the suite would be testing an empty database.
                new TenantGuardInterceptor(_tenancy, NullLogger<TenantGuardInterceptor>.Instance),
                new AuditingInterceptor(new TestUser(), new TestClock(), _tenancy))
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                    .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        var services = new ServiceCollection();
        services.AddSingleton<IDbContextFactory<DentalDbContext>>(new TestContextFactory(_options));
        services.AddTestTenancy();
        _provider = services.BuildServiceProvider();

        Seed();
    }

    private void Seed()
    {
        using var db = new DentalDbContext(_options, _tenancy);
        db.Database.EnsureCreated();

        // The grant lives in the database as role claims, exactly as it does in
        // a running system, so these tests exercise the real lookup path.
        foreach (var (roleName, permissions) in RolePermissions.Defaults)
        {
            var role = new ApplicationRole(roleName) { Id = Guid.NewGuid().ToString() };
            db.Roles.Add(role);

            foreach (var permission in permissions)
            {
                db.RoleClaims.Add(new Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>
                {
                    RoleId = role.Id,
                    ClaimType = PermissionCatalogue.ClaimType,
                    ClaimValue = permission
                });
            }
        }

        var patient = new Patient
        {
            PatientNumber = "P-000001",
            Name = new PersonName { FirstName = "Test", LastName = "Patient" },
            DateOfBirth = new DateOnly(1980, 1, 1),
            Status = PatientStatus.Active
        };

        db.Patients.Add(patient);
        db.SaveChanges();

        _patientId = patient.Id;
    }

    private IPermissionGuard GuardFor(params string[] roles)
    {
        var catalogue = new PermissionCatalogue(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PermissionCatalogue>.Instance);

        return new PermissionGuard(new RoleUser(roles), _tenancy, catalogue, NullLogger<PermissionGuard>.Instance);
    }

    private PatientService PatientsAs(params string[] roles) =>
        new(new DentalDbContext(_options, _tenancy), new StubSequences(), new TestClock(),
            NullLogger<PatientService>.Instance, GuardFor(roles));

    private ClinicalService ClinicalAs(params string[] roles) =>
        new(new DentalDbContext(_options, _tenancy), new StubSequences(), new TestUser(), new TestClock(),
            NullLogger<ClinicalService>.Instance, GuardFor(roles));

    private BillingService BillingAs(params string[] roles) =>
        new(new DentalDbContext(_options, _tenancy), new StubSequences(), new TestUser(), new TestClock(),
            NullLogger<BillingService>.Instance, GuardFor(roles));

    // ---------------------------------------------------------------- reception

    [Fact]
    public async Task Reception_may_open_the_patient_register()
    {
        var patients = PatientsAs(Roles.Receptionist);

        var result = await patients.SearchAsync(new PatientSearchCriteria());

        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task Reception_is_refused_the_dental_chart()
    {
        var clinical = ClinicalAs(Roles.Receptionist);

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => clinical.GetChartAsync(_patientId));

        Assert.Equal(Permissions.DentalChartView, refusal.Permission);
    }

    [Fact]
    public async Task Reception_is_refused_the_clinical_notes()
    {
        var clinical = ClinicalAs(Roles.Receptionist);

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => clinical.GetNotesAsync(_patientId));

        Assert.Equal(Permissions.ClinicalRecordsView, refusal.Permission);
    }

    [Fact]
    public async Task Reception_is_refused_the_medical_risk_assessment()
    {
        var patients = PatientsAs(Roles.Receptionist);

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => patients.GetRiskProfileAsync(_patientId));

        Assert.Equal(Permissions.MedicalHistoryView, refusal.Permission);
    }

    [Fact]
    public async Task Reception_is_refused_a_refund()
    {
        var billing = BillingAs(Roles.Receptionist);

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => billing.RefundPaymentAsync(Guid.NewGuid(), 10m, "test"));

        Assert.Equal(Permissions.BillingRefund, refusal.Permission);
    }

    // ---------------------------------------------------------------- management

    [Fact]
    public async Task A_practice_manager_is_refused_the_clinical_notes()
    {
        var clinical = ClinicalAs(Roles.PracticeManager);

        await Assert.ThrowsAsync<ForbiddenException>(() => clinical.GetNotesAsync(_patientId));
    }

    [Fact]
    public async Task A_practice_manager_may_refund_a_payment()
    {
        var billing = BillingAs(Roles.PracticeManager);

        // Refused for a missing payment, not for a missing permission: the guard
        // has already let it through by the time the record is looked up.
        var result = await billing.RefundPaymentAsync(Guid.NewGuid(), 10m, "test");

        Assert.False(result.Succeeded);
    }

    // ---------------------------------------------------------------- clinical

    [Fact]
    public async Task A_dentist_may_read_the_chart()
    {
        var clinical = ClinicalAs(Roles.Dentist);

        var chart = await clinical.GetChartAsync(_patientId);

        Assert.NotNull(chart);
    }

    [Fact]
    public async Task A_hygienist_is_refused_prescribing()
    {
        var clinical = ClinicalAs(Roles.Hygienist);

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => clinical.IssuePrescriptionAsync(new Prescription { PatientId = _patientId }, acknowledgeWarnings: true));

        Assert.Equal(Permissions.PrescriptionsCreate, refusal.Permission);
    }

    // ---------------------------------------------------------------- no role

    [Fact]
    public async Task An_account_with_no_role_can_read_nothing()
    {
        var patients = PatientsAs();

        var refusal = await Assert.ThrowsAsync<ForbiddenException>(
            () => patients.SearchAsync(new PatientSearchCriteria()));

        Assert.Equal(Permissions.PatientsView, refusal.Permission);
    }

    [Fact]
    public async Task An_account_with_no_role_cannot_create_a_patient()
    {
        var patients = PatientsAs();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => patients.CreateAsync(new Patient
            {
                Name = new PersonName { FirstName = "Walk", LastName = "In" },
                DateOfBirth = new DateOnly(1990, 1, 1)
            }));
    }

    [Fact]
    public async Task An_unrecognised_role_grants_nothing()
    {
        // A role present on the principal but absent from the grant table — the
        // shape a stale cookie or a hand-crafted token takes — must not be
        // treated as permissive.
        var patients = PatientsAs("SuperUser", "root", Roles.Administrator + "s");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => patients.SearchAsync(new PatientSearchCriteria()));
    }

    [Fact]
    public async Task Holding_two_roles_grants_the_union_and_no_more()
    {
        // Reception plus hygiene: may read the chart (hygiene) and register a
        // patient (reception), still may not refund (neither).
        var clinical = ClinicalAs(Roles.Receptionist, Roles.Hygienist);
        Assert.NotNull(await clinical.GetChartAsync(_patientId));

        var billing = BillingAs(Roles.Receptionist, Roles.Hygienist);
        await Assert.ThrowsAsync<ForbiddenException>(
            () => billing.RefundPaymentAsync(Guid.NewGuid(), 5m, "test"));
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    /// <summary>A principal holding exactly the roles a test names.</summary>
    private sealed class RoleUser(string[] roles) : ICurrentUser
    {
        public string? UserId => "authz-test";
        public string? UserName => "authz@dentalsurgery.local";
        public string? DisplayName => "Authorisation Test";
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

// ---------------------------------------------------------------------------
// Return-URL handling on the sign-in page.
// ---------------------------------------------------------------------------

public class ReturnUrlTests
{
    private const string BaseUri = "https://practice.example/";

    /// <summary>
    /// Mirrors IdentityRedirectManager.MakeLocal. Asserted here as well as in
    /// the integration suite because the rules are subtle and the cost of
    /// getting them wrong runs in both directions: too permissive is an open
    /// redirect, too strict silently sends every internal redirect — the lockout
    /// page, the two-factor page — to the home page instead.
    /// </summary>
    private static string Local(string? candidate)
    {
        const string home = "/";

        if (string.IsNullOrWhiteSpace(candidate)) return home;

        var trimmed = candidate.Trim();
        var normalised = trimmed.Replace('\\', '/');

        if (normalised.Any(char.IsControl)) return home;
        if (LooksLikeAnAuthority(normalised)) return home;

        var baseUri = new Uri(BaseUri);

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        {
            return SameOrigin(absolute, baseUri)
                ? absolute.PathAndQuery + absolute.Fragment
                : home;
        }

        Uri resolved;
        try { resolved = new Uri(baseUri, normalised); }
        catch (UriFormatException) { return home; }

        return SameOrigin(resolved, baseUri)
            ? resolved.PathAndQuery + resolved.Fragment
            : home;
    }

    private static bool LooksLikeAnAuthority(string value)
    {
        var current = value;

        for (var pass = 0; pass < 3; pass++)
        {
            if (current.StartsWith("//", StringComparison.Ordinal)) return true;

            var decoded = Uri.UnescapeDataString(current).Replace('\\', '/');
            if (decoded == current) break;
            current = decoded;
        }

        return current.StartsWith("//", StringComparison.Ordinal);
    }

    private static bool SameOrigin(Uri candidate, Uri baseUri) =>
        candidate.Scheme == baseUri.Scheme
        && candidate.Authority == baseUri.Authority
        && candidate.AbsolutePath.StartsWith(baseUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);

    [Theory]
    [InlineData("//evil.example")]
    [InlineData("\\\\evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("https://evil.example")]
    [InlineData("http://evil.example/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/%5C%5Cevil.example")]
    [InlineData("/%2F%2Fevil.example")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void A_target_that_leaves_this_origin_is_replaced_with_the_home_page(string? candidate)
    {
        Assert.Equal("/", Local(candidate));
    }

    [Theory]
    [InlineData("/patients", "/patients")]
    [InlineData("/patients/new", "/patients/new")]
    [InlineData("/billing/invoices?status=Issued", "/billing/invoices?status=Issued")]
    public void A_target_inside_the_application_survives(string candidate, string expected)
    {
        Assert.Equal(expected, Local(candidate));
    }

    [Fact]
    public void A_bare_word_is_a_path_on_this_site_and_goes_nowhere_else()
    {
        // "evil.example" carries no scheme and no authority, so a browser reads
        // it as a path relative to the current site. Resolving it locally is the
        // correct answer, and the destination is this application either way.
        Assert.Equal("/evil.example", Local("evil.example"));
    }

    [Theory]
    [InlineData("Account/Lockout", "/Account/Lockout")]
    [InlineData("Account/LoginWith2fa", "/Account/LoginWith2fa")]
    [InlineData("Account/Manage/SetPassword", "/Account/Manage/SetPassword")]
    public void A_relative_target_from_the_identity_pages_survives(string candidate, string expected)
    {
        // Regression. These are the form the identity pages actually use, and an
        // earlier version of the guard rejected them for having no leading
        // slash — which sent a locked-out user to the home page rather than to
        // the page telling them they were locked out.
        Assert.Equal(expected, Local(candidate));
    }

    [Theory]
    [InlineData("https://practice.example/patients", "/patients")]
    [InlineData("https://practice.example/Account/LoginWith2fa?rememberMe=False",
                "/Account/LoginWith2fa?rememberMe=False")]
    public void An_absolute_target_on_this_origin_survives_as_a_path(string candidate, string expected)
    {
        // The query-parameter overload builds an absolute URL before redirecting.
        Assert.Equal(expected, Local(candidate));
    }

    [Fact]
    public void The_scaffolded_check_this_replaced_would_have_allowed_a_protocol_relative_url()
    {
        // Kept as a regression marker: this is exactly the case that made the
        // original implementation an open redirect.
        Assert.True(Uri.IsWellFormedUriString("//evil.example", UriKind.Relative));
        Assert.Equal("/", Local("//evil.example"));
    }
}
