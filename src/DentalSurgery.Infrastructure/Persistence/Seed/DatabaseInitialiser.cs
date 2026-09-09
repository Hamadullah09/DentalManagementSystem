using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Services;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DentalSurgery.Infrastructure.Persistence.Seed;

/// <summary>
/// Brings a database up to a usable state: schema, reference catalogues, roles
/// and the first administrator, plus optional demonstration data.
/// Every step is idempotent, so it is safe to run on every start-up.
/// <para>
/// The split matters. Tooth positions and procedure codes are reference data and
/// always seed. The practice, its staff and their logins are <em>demonstration</em>
/// data and seed only when demo data is explicitly enabled, because those logins
/// share a documented password. A production instance gets catalogues and one
/// administrator, nothing more.
/// </para>
/// </summary>
public class DatabaseInitialiser(
    DentalDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<SeedOptions> seedOptions,
    IHostEnvironment environment,
    ITenantScopeFactory tenantScopeFactory,
    TenantProvisioningService tenants,
    ILogger<DatabaseInitialiser> logger)
{
    /// <summary>
    /// The password given to demonstration logins. It is deliberately public and
    /// documented: it is only ever applied to fabricated staff in a demo build,
    /// and <see cref="SeedOptions.DemoData"/> is off by default so it cannot
    /// reach an instance holding real records.
    /// </summary>
    public const string DemoPassword = "Dental#2026!";

    private readonly SeedOptions _options = seedOptions.Value;

    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        var demo = ResolveDemoDataMode();

        // Schema, roles and the shared clinical catalogues belong to no tenant,
        // so they are written from a platform scope.
        using (tenantScopeFactory.EnterPlatformScope("database initialisation"))
        {
            await PrepareSchemaAsync(ct);
            await SeedReferenceDataAsync(ct);
        }

        // Everything from here belongs to a practice, so a tenant has to exist
        // before it can be written. On a fresh install this creates the first
        // one; afterwards it returns the same tenant unchanged.
        var tenant = await tenants.EnsureTenantAsync(
            _options.TenantName, _options.TenantSlug, ct);

        using (tenantScopeFactory.EnterTenant(tenant.Id))
        {
            // Roles and their permission grants belong to the tenant: each
            // practice owns its own copy and may edit it without affecting
            // anyone else.
            await LoadKnownPermissionsAsync(ct);
            await SeedRolesAsync(ct);
            await SeedAdministratorAsync(tenant.Id, ct);

            if (demo)
            {
                await SeedPracticeAsync(ct);
                await SeedStaffAndUsersAsync(tenant.Id, ct);
                await SeedOperationalDataAsync(ct);
                await new DemoDataBuilder(db, logger).BuildAsync(ct);
            }
        }

        logger.LogInformation(
            "Database initialisation complete for tenant {Tenant} ({Slug}).", tenant.Name, tenant.Slug);
    }

    /// <summary>
    /// Decides whether demonstration data may be created, and says plainly why
    /// when it refuses. Demo data outside Development needs a second, separate
    /// opt-in, so that enabling it for a training instance is a deliberate act
    /// rather than a setting inherited from a developer's file.
    /// </summary>
    private bool ResolveDemoDataMode()
    {
        if (!_options.DemoData) return false;

        if (environment.IsDevelopment()) return true;

        if (_options.AllowDemoDataOutsideDevelopment)
        {
            logger.LogWarning(
                "Demonstration data is enabled in the {Environment} environment. This creates staff " +
                "logins on a shared, publicly documented password. This instance must never hold real " +
                "patient records.", environment.EnvironmentName);
            return true;
        }

        logger.LogWarning(
            "Seed:DemoData is set but the environment is {Environment}, not Development. Demonstration " +
            "data has been skipped because it creates logins on a shared password. Set " +
            "Seed:AllowDemoDataOutsideDevelopment only if this instance holds no real data.",
            environment.EnvironmentName);
        return false;
    }

    /// <summary>
    /// Applies migrations, or verifies that someone else already has.
    /// <para>
    /// Migrating on start-up is convenient for a single instance and wrong for
    /// several: hosts starting together race on the same schema. The default is
    /// therefore to verify and refuse, so a deployment that skipped its
    /// migration step fails immediately and visibly instead of throwing column
    /// errors across unrelated screens for the next hour.
    /// </para>
    /// </summary>
    private async Task PrepareSchemaAsync(CancellationToken ct)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database schema is up to date.");
            return;
        }

        if (_options.MigrateOnStartup)
        {
            logger.LogInformation("Applying {Count} migration(s): {Names}", pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync(ct);
            return;
        }

        if (!_options.VerifySchemaOnStartup)
        {
            logger.LogWarning(
                "{Count} migration(s) are pending and both Seed:MigrateOnStartup and " +
                "Seed:VerifySchemaOnStartup are off. Running against an out-of-date schema: {Names}",
                pending.Count, string.Join(", ", pending));
            return;
        }

        throw new InvalidOperationException(
            $"The database is missing {pending.Count} migration(s): {string.Join(", ", pending)}. " +
            "Apply them as a release step ('dotnet ef database update', or the generated SQL script), " +
            "or set Seed:MigrateOnStartup=true for a single-instance install.");
    }

    // ------------------------------------------------------------------ roles

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var descriptions = new Dictionary<string, string>
        {
            [Roles.Administrator] = "Unrestricted access, including system configuration and user management.",
            [Roles.PracticeManager] = "Operational management, reporting and staff administration.",
            [Roles.Dentist] = "Full clinical access including charting, prescribing and treatment planning.",
            [Roles.OralSurgeon] = "Clinical access plus surgical records, anaesthesia and the implant registry.",
            [Roles.Hygienist] = "Charting, periodontal assessment and hygiene treatment.",
            [Roles.Nurse] = "Chairside support, stock and sterilisation records.",
            [Roles.Receptionist] = "Appointments, patient registration and payments.",
            [Roles.Accounts] = "Invoicing, payments, claims and financial reporting.",
            [Roles.ReadOnly] = "View-only access for audit and training."
        };

        var order = 0;
        foreach (var name in Roles.All)
        {
            order += 10;
            if (await roleManager.RoleExistsAsync(name)) continue;

            var result = await roleManager.CreateAsync(new ApplicationRole(name)
            {
                Description = descriptions.GetValueOrDefault(name),
                IsSystemRole = true,
                SortOrder = order
            });

            if (!result.Succeeded)
                logger.LogError("Could not create role {Role}: {Errors}", name,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await SeedRolePermissionsAsync(ct);
    }

    /// <summary>
    /// Writes the default permission grant for each role.
    /// <para>
    /// Grants are stored as role claims so an administrator can change them at
    /// runtime. That makes this an initialiser, not a synchroniser: a role that
    /// already has grants is left exactly as configured, because overwriting it
    /// on every start would silently undo deliberate local policy. The one thing
    /// that is reconciled is a permission this build has introduced and no role
    /// yet mentions — without that, a new capability would ship with nobody,
    /// including the administrator, able to use it.
    /// </para>
    /// </summary>
    private async Task SeedRolePermissionsAsync(CancellationToken ct)
    {
        var granted = 0;

        foreach (var name in Roles.All)
        {
            var role = await roleManager.FindByNameAsync(name);
            if (role is null) continue;

            var existing = (await roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionCatalogue.ClaimType)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.Ordinal);

            var defaults = RolePermissions.For(name);

            // Never configured: lay down the whole default grant.
            // Already configured: only add permissions that did not exist when
            // it was configured, so local edits survive an upgrade.
            var wanted = existing.Count == 0
                ? defaults
                : defaults.Where(p => !KnownPermissions.Contains(p)).ToArray();

            foreach (var permission in wanted.Where(p => !existing.Contains(p)))
            {
                var result = await roleManager.AddClaimAsync(
                    role, new Claim(PermissionCatalogue.ClaimType, permission));

                if (result.Succeeded) granted++;
                else
                    logger.LogError("Could not grant {Permission} to {Role}: {Errors}",
                        permission, name, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        // Record what this build knows about, so the next start can tell a
        // brand-new permission from one an administrator deliberately revoked.
        await RecordKnownPermissionsAsync(ct);

        if (granted > 0) logger.LogInformation("Applied {Count} role permission grant(s).", granted);
    }

    private HashSet<string> KnownPermissions { get; set; } = new(StringComparer.Ordinal);

    private async Task LoadKnownPermissionsAsync(CancellationToken ct)
    {
        var record = await db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == KnownPermissionsSetting, ct);

        KnownPermissions = string.IsNullOrWhiteSpace(record?.Value)
            ? new HashSet<string>(StringComparer.Ordinal)
            : record.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);
    }

    private async Task RecordKnownPermissionsAsync(CancellationToken ct)
    {
        var value = string.Join(',', Permissions.All);

        var record = await db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == KnownPermissionsSetting, ct);

        if (record is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = KnownPermissionsSetting,
                Value = value,
                Category = "Security",
                Description = "Permissions defined by the deployed build. Maintained automatically.",
                IsSystem = true
            });
        }
        else
        {
            record.Value = value;
        }

        await db.SaveChangesAsync(ct);
        KnownPermissions = Permissions.All.ToHashSet(StringComparer.Ordinal);
    }

    private const string KnownPermissionsSetting = "Security.KnownPermissions";

    // ------------------------------------------------------------------ reference data

    private async Task SeedReferenceDataAsync(CancellationToken ct)
    {
        if (!await db.Teeth.AnyAsync(ct))
        {
            var teeth = ToothReferenceData.Build();
            db.Teeth.AddRange(teeth);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} tooth positions.", teeth.Count);
        }

        if (!await db.ProcedureCodes.AnyAsync(ct))
        {
            var codes = ProcedureCatalogue.Build();
            db.ProcedureCodes.AddRange(codes);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} procedure codes.", codes.Count);
        }

        if (!await db.MedicalConditions.AnyAsync(ct))
        {
            db.MedicalConditions.AddRange(ClinicalCatalogues.MedicalConditions());
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Allergens.AnyAsync(ct))
        {
            db.Allergens.AddRange(ClinicalCatalogues.Allergens());
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Medications.AnyAsync(ct))
        {
            db.Medications.AddRange(ClinicalCatalogues.Medications());
            await db.SaveChangesAsync(ct);
        }

        if (!await db.ConsentFormTemplates.AnyAsync(ct))
        {
            db.ConsentFormTemplates.AddRange(ClinicalCatalogues.ConsentForms());
            await db.SaveChangesAsync(ct);
        }

        if (!await db.PostOperativeInstructions.AnyAsync(ct))
        {
            db.PostOperativeInstructions.AddRange(ClinicalCatalogues.PostOperativeInstructions());
            await db.SaveChangesAsync(ct);
        }

        if (!await db.MessageTemplates.AnyAsync(ct))
        {
            db.MessageTemplates.AddRange(ClinicalCatalogues.MessageTemplates());
            await db.SaveChangesAsync(ct);
        }

        if (!await db.NumberSequences.AnyAsync(ct))
        {
            var names = new[]
            {
                SequenceNames.Patient, SequenceNames.Staff, SequenceNames.Appointment,
                SequenceNames.Invoice, SequenceNames.Payment, SequenceNames.TreatmentPlan,
                SequenceNames.Claim, SequenceNames.Prescription, SequenceNames.LabCase,
                SequenceNames.PurchaseOrder, SequenceNames.Referral, SequenceNames.PaymentPlan
            };
            db.NumberSequences.AddRange(names.Select(NumberSequenceService.CreateDefault));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.AppSettings.AnyAsync(ct))
        {
            db.AppSettings.AddRange(DefaultSettings());
            await db.SaveChangesAsync(ct);
        }
    }

    private static List<AppSetting> DefaultSettings() => new()
    {
        new() { Key = "Scheduling.SlotMinutes", Value = "15", Category = "Scheduling", DataType = "int",
                Description = "Granularity of the appointment grid, in minutes." },
        new() { Key = "Scheduling.DayStart", Value = "08:00", Category = "Scheduling", DataType = "time",
                Description = "First slot shown on the appointment book." },
        new() { Key = "Scheduling.DayEnd", Value = "18:30", Category = "Scheduling", DataType = "time",
                Description = "Last slot shown on the appointment book." },
        new() { Key = "Scheduling.ReminderHours", Value = "24", Category = "Scheduling", DataType = "int",
                Description = "How far ahead the default SMS reminder is sent." },
        new() { Key = "Clinical.MedicalHistoryValidMonths", Value = "12", Category = "Clinical", DataType = "int",
                Description = "How long a medical history review stays current." },
        new() { Key = "Clinical.RequireNoteSignature", Value = "true", Category = "Clinical", DataType = "bool",
                Description = "Require clinical notes to be signed before the visit is closed." },
        new() { Key = "Clinical.DefaultRecallMonths", Value = "6", Category = "Clinical", DataType = "int",
                Description = "Default recall interval for new patients." },
        new() { Key = "Billing.PaymentTermDays", Value = "30", Category = "Billing", DataType = "int",
                Description = "Days from issue until an invoice falls due." },
        new() { Key = "Billing.AutoIssueInvoices", Value = "false", Category = "Billing", DataType = "bool",
                Description = "Issue invoices automatically when a procedure is completed." },
        new() { Key = "Billing.TaxRatePercent", Value = "0", Category = "Billing", DataType = "decimal",
                Description = "Tax applied to taxable procedures. Most dental treatment is exempt." },
        new() { Key = "Inventory.ExpiryWarningDays", Value = "90", Category = "Inventory", DataType = "int",
                Description = "How far ahead expiring stock is flagged." },
        new() { Key = "Sterilisation.SterilityMonths", Value = "12", Category = "Sterilisation", DataType = "int",
                Description = "Shelf life of a pouched sterile instrument set." },
        new() { Key = "Security.SessionTimeoutMinutes", Value = "30", Category = "Security", DataType = "int",
                Description = "Idle time before a session is signed out." },
        new() { Key = "Security.AuditRetentionYears", Value = "10", Category = "Security", DataType = "int",
                Description = "How long audit records are kept." }
    };

    // ------------------------------------------------------------------ practice

    private async Task SeedPracticeAsync(CancellationToken ct)
    {
        if (await db.Practices.AnyAsync(ct)) return;

        var practice = new Practice
        {
            Name = "Meridian Dental Surgery",
            LegalName = "Meridian Dental Care Limited",
            RegistrationNumber = "09482716",
            RegulatorNumber = "CQC-1-2049183726",
            Address = new Address
            {
                Line1 = "42 Harley Mews", City = "London",
                County = "Greater London", PostCode = "W1G 8QT", Country = "United Kingdom"
            },
            Contact = new ContactDetails
            {
                MobilePhone = "07700 900812",
                HomePhone = "020 7946 0812",
                Email = "reception@meridiandental.example",
                PreferredContactMethod = "Email"
            },
            Website = "https://meridiandental.example",
            CurrencyCode = "GBP",
            CurrencySymbol = "£",
            TimeZoneId = "GMT Standard Time",
            DefaultAppointmentMinutes = 30,
            DefaultRecallIntervalMonths = 6,
            InvoicePaymentTermDays = 30,
            InvoiceFooterText = "Payment is due within 30 days. Most dental treatment is exempt from VAT.",
            BankDetails = "Meridian Dental Care Ltd - Sort code 20-00-00 - Account 12345678"
        };

        // The main site sits at the practice address, but owned types must not be
        // shared between two entities or a single edit would write to both rows.
        var main = new Location
        {
            PracticeId = practice.Id,
            Code = "HARLEY",
            Name = "Harley Mews (Main Surgery)",
            Address = new Address
            {
                Line1 = practice.Address.Line1, City = practice.Address.City,
                County = practice.Address.County, PostCode = practice.Address.PostCode,
                Country = practice.Address.Country
            },
            Contact = new ContactDetails
            {
                MobilePhone = practice.Contact.MobilePhone,
                HomePhone = practice.Contact.HomePhone,
                Email = practice.Contact.Email,
                PreferredContactMethod = practice.Contact.PreferredContactMethod
            },
            IsPrimary = true,
            ColourHex = "#0d6efd",
            Notes = "Six surgeries including a dedicated implant suite with sedation facilities."
        };

        var satellite = new Location
        {
            PracticeId = practice.Id,
            Code = "RIVERSIDE",
            Name = "Riverside Clinic",
            Address = new Address
            {
                Line1 = "8 Wharf Road", City = "London",
                County = "Greater London", PostCode = "SE1 2NB", Country = "United Kingdom"
            },
            Contact = new ContactDetails
            {
                HomePhone = "020 7946 0977",
                Email = "riverside@meridiandental.example",
                PreferredContactMethod = "Email"
            },
            ColourHex = "#20c997",
            Notes = "Two surgeries. Hygiene and routine restorative work only."
        };

        practice.Locations.Add(main);
        practice.Locations.Add(satellite);

        // Opening hours
        var weekdayHours = new (DayOfWeek Day, TimeSpan Open, TimeSpan Close, bool Closed)[]
        {
            (DayOfWeek.Monday, new(8, 30, 0), new(18, 0, 0), false),
            (DayOfWeek.Tuesday, new(8, 30, 0), new(18, 0, 0), false),
            (DayOfWeek.Wednesday, new(8, 30, 0), new(19, 30, 0), false),
            (DayOfWeek.Thursday, new(8, 30, 0), new(18, 0, 0), false),
            (DayOfWeek.Friday, new(8, 30, 0), new(16, 30, 0), false),
            (DayOfWeek.Saturday, new(9, 0, 0), new(13, 0, 0), false),
            (DayOfWeek.Sunday, new(0, 0, 0), new(0, 0, 0), true)
        };

        foreach (var (day, open, close, closed) in weekdayHours)
        {
            main.BusinessHours.Add(new BusinessHours
            {
                LocationId = main.Id, DayOfWeek = day, OpenTime = open, CloseTime = close, IsClosed = closed,
                BreakStart = closed ? null : new TimeSpan(13, 0, 0),
                BreakEnd = closed ? null : new TimeSpan(14, 0, 0)
            });

            satellite.BusinessHours.Add(new BusinessHours
            {
                LocationId = satellite.Id, DayOfWeek = day,
                OpenTime = new TimeSpan(9, 0, 0), CloseTime = new TimeSpan(17, 0, 0),
                IsClosed = closed || day == DayOfWeek.Saturday
            });
        }

        // Surgeries
        var operatories = new (string Code, string Name, bool Surgical, bool Xray, bool Scanner, bool Sedation, string Colour)[]
        {
            ("S1", "Surgery 1", false, true, false, false, "#0d6efd"),
            ("S2", "Surgery 2", false, true, false, false, "#6610f2"),
            ("S3", "Surgery 3", false, true, true, false, "#6f42c1"),
            ("S4", "Surgery 4 - Hygiene", false, false, false, false, "#20c997"),
            ("S5", "Surgery 5 - Implant Suite", true, true, true, true, "#fd7e14"),
            ("S6", "Surgery 6 - Oral Surgery", true, true, false, true, "#dc3545")
        };

        var order = 0;
        foreach (var (code, name, surgical, xray, scanner, sedation, colour) in operatories)
        {
            main.Operatories.Add(new Operatory
            {
                LocationId = main.Id, Code = code, Name = name, DisplayOrder = order += 10,
                IsSurgicalSuite = surgical, HasXRay = xray, HasIntraoralScanner = scanner,
                HasSedationEquipment = sedation, ColourHex = colour,
                EquipmentNotes = surgical
                    ? "Surgical motor, piezo unit, physiodispenser and full monitoring."
                    : "Standard chair, curing light and ultrasonic scaler."
            });
        }

        satellite.Operatories.Add(new Operatory
        {
            LocationId = satellite.Id, Code = "R1", Name = "Riverside Surgery 1",
            DisplayOrder = 10, HasXRay = true, ColourHex = "#0dcaf0"
        });
        satellite.Operatories.Add(new Operatory
        {
            LocationId = satellite.Id, Code = "R2", Name = "Riverside Surgery 2 - Hygiene",
            DisplayOrder = 20, ColourHex = "#198754"
        });

        db.Practices.Add(practice);
        await db.SaveChangesAsync(ct);

        // Fee schedules
        var privateSchedule = new FeeSchedule
        {
            Name = "Private Fee Schedule 2026",
            Description = "Standard private fees, effective January 2026.",
            ScheduleType = FeeScheduleType.Practice,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            IsDefault = true
        };

        var planSchedule = new FeeSchedule
        {
            Name = "Membership Plan Rates",
            Description = "Discounted rates for patients on the practice membership plan.",
            ScheduleType = FeeScheduleType.Discount,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            BlanketAdjustmentPercent = -15m
        };

        db.FeeSchedules.AddRange(privateSchedule, planSchedule);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded practice, {Locations} locations and {Operatories} surgeries.",
            2, main.Operatories.Count + satellite.Operatories.Count);
    }

    // ------------------------------------------------------------------ staff and logins

    private async Task SeedStaffAndUsersAsync(Guid tenantId, CancellationToken ct)
    {
        if (await db.Staff.AnyAsync(ct)) return;

        var main = await db.Locations.FirstAsync(l => l.Code == "HARLEY", ct);
        var satellite = await db.Locations.FirstAsync(l => l.Code == "RIVERSIDE", ct);
        var operatories = await db.Operatories.Where(o => o.LocationId == main.Id)
            .OrderBy(o => o.DisplayOrder).ToListAsync(ct);

        var definitions = new List<(
            string Number, string Title, string First, string Last, StaffRole Role, string Specialty,
            bool Provider, bool Prescribe, bool Surgery, bool Sedation, string Colour, string Email, string AppRole,
            string? Operatory)>
        {
            ("S-0001", "Dr", "Amara", "Okonkwo", StaffRole.Dentist, "General and restorative dentistry",
                true, true, true, false, "#0d6efd", "a.okonkwo@meridiandental.example", Roles.Dentist, "S1"),
            ("S-0002", "Dr", "Rhys", "Llewellyn", StaffRole.OralSurgeon, "Oral and maxillofacial surgery, implantology",
                true, true, true, true, "#dc3545", "r.llewellyn@meridiandental.example", Roles.OralSurgeon, "S5"),
            ("S-0003", "Dr", "Priya", "Raghunathan", StaffRole.Periodontist, "Periodontology and peri-implant care",
                true, true, true, false, "#6f42c1", "p.raghunathan@meridiandental.example", Roles.Dentist, "S3"),
            ("S-0004", "Dr", "Tomas", "Bergqvist", StaffRole.Endodontist, "Endodontics and microsurgery",
                true, true, true, false, "#fd7e14", "t.bergqvist@meridiandental.example", Roles.Dentist, "S2"),
            ("S-0005", "Ms", "Fiona", "Kelleher", StaffRole.DentalHygienist, "Periodontal therapy and prevention",
                true, false, false, false, "#20c997", "f.kelleher@meridiandental.example", Roles.Hygienist, "S4"),
            ("S-0006", "Mr", "Daniel", "Osei", StaffRole.DentalTherapist, "Direct restorations and paediatric care",
                true, false, false, false, "#198754", "d.osei@meridiandental.example", Roles.Hygienist, "S4"),
            ("S-0007", "Ms", "Hana", "Sato", StaffRole.DentalNurse, "Implant and surgical nursing",
                false, false, false, false, "#6c757d", "h.sato@meridiandental.example", Roles.Nurse, null),
            ("S-0008", "Mr", "Callum", "Reid", StaffRole.DentalNurse, "Radiography and decontamination lead",
                false, false, false, false, "#6c757d", "c.reid@meridiandental.example", Roles.Nurse, null),
            ("S-0009", "Mrs", "Beatrice", "Nwosu", StaffRole.PracticeManager, "Practice management and compliance",
                false, false, false, false, "#0dcaf0", "b.nwosu@meridiandental.example", Roles.PracticeManager, null),
            ("S-0010", "Ms", "Jodie", "Barnes", StaffRole.Receptionist, "Front desk and patient coordination",
                false, false, false, false, "#adb5bd", "j.barnes@meridiandental.example", Roles.Receptionist, null),
            ("S-0011", "Mr", "Idris", "Farouk", StaffRole.TreatmentCoordinator, "Treatment planning and finance",
                false, false, false, false, "#ffc107", "i.farouk@meridiandental.example", Roles.Accounts, null)
        };

        var staffList = new List<Staff>();

        foreach (var d in definitions)
        {
            var staff = new Staff
            {
                StaffNumber = d.Number,
                Name = new PersonName { Title = d.Title, FirstName = d.First, LastName = d.Last },
                Contact = new ContactDetails { Email = d.Email, MobilePhone = "07700 900" + d.Number[^3..], PreferredContactMethod = "Email" },
                Address = new Address { Line1 = "42 Harley Mews", City = "London", PostCode = "W1G 8QT", Country = "United Kingdom" },
                Role = d.Role,
                Specialty = d.Specialty,
                JobTitle = d.Specialty,
                IsProvider = d.Provider,
                CanPrescribe = d.Prescribe,
                CanPerformSurgery = d.Surgery,
                CanAdministerSedation = d.Sedation,
                CanTakeRadiographs = d.Role is StaffRole.Dentist or StaffRole.OralSurgeon or StaffRole.Periodontist
                    or StaffRole.Endodontist or StaffRole.DentalNurse or StaffRole.Radiographer,
                ColourHex = d.Colour,
                DefaultLocationId = main.Id,
                EmploymentType = d.Role == StaffRole.Dentist ? EmploymentType.Associate : EmploymentType.FullTime,
                HireDate = new DateOnly(2019 + (staffList.Count % 6), 1 + (staffList.Count % 11), 1 + (staffList.Count % 27)),
                RegistrationNumber = d.Provider ? $"GDC-{200000 + staffList.Count * 137}" : null,
                RegistrationExpiry = d.Provider ? new DateOnly(2026, 7, 31) : null,
                IndemnityProvider = d.Provider ? "Dental Protection" : null,
                IndemnityExpiry = d.Provider ? new DateOnly(2026, 12, 31) : null,
                DefaultAppointmentMinutes = d.Role switch
                {
                    StaffRole.DentalHygienist => 40,
                    StaffRole.OralSurgeon => 60,
                    StaffRole.Endodontist => 90,
                    _ => 30
                },
                DailyProductionTarget = d.Provider ? 1800m : null,
                Qualifications = d.Role switch
                {
                    StaffRole.OralSurgeon => "BDS, MFDS RCS(Eng), MSc Oral Surgery",
                    StaffRole.Periodontist => "BDS, MSc Periodontology, MRD RCS",
                    StaffRole.Endodontist => "BDS, MSc Endodontology",
                    StaffRole.Dentist => "BDS, MJDF RCS(Eng)",
                    StaffRole.DentalHygienist => "Dip Dental Hygiene, BSc Oral Health Science",
                    StaffRole.DentalTherapist => "BSc Dental Therapy and Hygiene",
                    _ => null
                }
            };

            staffList.Add(staff);
        }

        db.Staff.AddRange(staffList);
        await db.SaveChangesAsync(ct);

        // Working rotas for the providers
        var weekdays = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        };

        foreach (var (definition, staff) in definitions.Zip(staffList))
        {
            if (!staff.IsProvider) continue;

            var operatory = definition.Operatory is null
                ? null
                : operatories.FirstOrDefault(o => o.Code == definition.Operatory);

            // Associates work four days; salaried clinicians work five.
            var days = staff.EmploymentType == EmploymentType.Associate ? weekdays.Take(4) : weekdays;

            foreach (var day in days)
            {
                db.StaffScheduleSlots.Add(new StaffScheduleSlot
                {
                    StaffId = staff.Id,
                    LocationId = main.Id,
                    DefaultOperatoryId = operatory?.Id,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = day == DayOfWeek.Friday ? new TimeSpan(16, 0, 0) : new TimeSpan(17, 30, 0),
                    BreakStart = new TimeSpan(13, 0, 0),
                    BreakEnd = new TimeSpan(14, 0, 0),
                    EffectiveFrom = new DateOnly(2024, 1, 1)
                });
            }

            // The hygienist also covers the satellite clinic on Saturdays.
            if (staff.Role == StaffRole.DentalHygienist)
            {
                db.StaffScheduleSlots.Add(new StaffScheduleSlot
                {
                    StaffId = staff.Id,
                    LocationId = satellite.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    EffectiveFrom = new DateOnly(2024, 1, 1)
                });
            }
        }

        await db.SaveChangesAsync(ct);

        // Demonstration logins. The administrator is created separately by
        // SeedAdministratorAsync, which runs in every environment and takes its
        // password from configuration.
        foreach (var (definition, staff) in definitions.Zip(staffList))
        {
            var roles = new[] { definition.AppRole };

            var user = await EnsureUserAsync(tenantId, definition.Email, DemoPassword,
                definition.First, definition.Last, roles, staff.Id, ct);

            if (user is not null)
            {
                staff.ApplicationUserId = user.Id;
                user.JobTitle = definition.Specialty;
                user.DefaultLocationId = main.Id;
                await userManager.UpdateAsync(user);
            }
        }

        // The staff numbers above are assigned directly, so the allocator must
        // start after them or the next hire would be given a duplicate.
        var staffSequence = await db.NumberSequences.FirstOrDefaultAsync(s => s.Name == SequenceNames.Staff, ct);
        if (staffSequence is not null && staffSequence.NextValue <= staffList.Count)
            staffSequence.NextValue = staffList.Count + 1;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} staff records with logins.", staffList.Count);
    }

    // ------------------------------------------------------------------ administrator

    /// <summary>
    /// Creates the first administrator if there is not one already.
    /// <para>
    /// Outside Development the password must come from configuration. There is
    /// no built-in fallback: an instance deployed without one fails to start,
    /// which is a far better outcome than an instance that starts with an
    /// administrator whose password is printed in the README.
    /// </para>
    /// <para>
    /// The account is always flagged <c>MustChangePassword</c>, so the
    /// bootstrap value cannot become a standing credential.
    /// </para>
    /// </summary>
    private async Task SeedAdministratorAsync(Guid tenantId, CancellationToken ct)
    {
        var email = string.IsNullOrWhiteSpace(_options.AdminEmail)
            ? "admin@dentalsurgery.local"
            : _options.AdminEmail.Trim();

        if (await userManager.FindByEmailAsync(email) is not null) return;

        // An instance that already has an administrator does not need another;
        // this keeps a rotated or renamed account from being silently re-created.
        if (await AnyAdministratorExistsAsync(ct))
        {
            logger.LogInformation("An administrator already exists; skipping bootstrap account {Email}.", email);
            return;
        }

        var password = _options.AdminPassword;

        if (string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "No administrator password is configured and the database has no administrator. " +
                    "Set Seed:AdminPassword through an environment variable or secret store " +
                    "(for example Seed__AdminPassword) before starting. It is required only for the " +
                    "first start, and the account is created with a forced password change.");
            }

            password = DemoPassword;
            logger.LogWarning(
                "Using the built-in development password for {Email}. This is permitted only in " +
                "Development; other environments require Seed:AdminPassword.", email);
        }

        var user = await EnsureUserAsync(tenantId, email, password!, "System", "Administrator",
            new[] { Roles.Administrator }, null, ct);

        if (user is null) return;

        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);

        logger.LogInformation(
            "Created the bootstrap administrator {Email}. The password must be changed at first sign-in.",
            email);
    }

    private async Task<bool> AnyAdministratorExistsAsync(CancellationToken ct)
    {
        var administrators = await userManager.GetUsersInRoleAsync(Roles.Administrator);
        return administrators.Count > 0;
    }

    private async Task<ApplicationUser?> EnsureUserAsync(
        Guid tenantId, string email, string password, string firstName, string lastName,
        IEnumerable<string> roles, Guid? staffId, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser
        {
            TenantId = tenantId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            StaffId = staffId,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("Could not create the login for {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRolesAsync(user, roles);
        return user;
    }

    // ------------------------------------------------------------------ operational data

    private async Task SeedOperationalDataAsync(CancellationToken ct)
    {
        var main = await db.Locations.FirstOrDefaultAsync(l => l.Code == "HARLEY", ct);
        if (main is null) return;

        if (!await db.Suppliers.AnyAsync(ct))
        {
            var suppliers = new List<Supplier>
            {
                new() { Name = "Henry Schein Dental UK", AccountNumber = "HS-40218", LeadTimeDays = 2,
                        IsPreferred = true, PaymentTerms = "30 days net",
                        Contact = new ContactDetails { HomePhone = "0800 023 2558", Email = "orders@example-hs.co.uk", PreferredContactMethod = "Email" },
                        Address = new Address { Line1 = "Medcare House", City = "Gillingham", PostCode = "ME8 0SB" } },
                new() { Name = "Dental Directory", AccountNumber = "DD-88104", LeadTimeDays = 3,
                        PaymentTerms = "30 days net",
                        Contact = new ContactDetails { HomePhone = "0800 585 586", Email = "sales@example-dd.co.uk", PreferredContactMethod = "Email" },
                        Address = new Address { Line1 = "6 Perry Way", City = "Witham", PostCode = "CM8 3SX" } },
                new() { Name = "Nobel Biocare UK", AccountNumber = "NB-21193", LeadTimeDays = 5,
                        MinimumOrderValue = 250m, PaymentTerms = "45 days net",
                        Contact = new ContactDetails { HomePhone = "0208 756 3300", Email = "uk.orders@example-nb.com", PreferredContactMethod = "Email" },
                        Address = new Address { Line1 = "Trident Business Park", City = "Uxbridge", PostCode = "UB8 1LR" } },
                new() { Name = "Initial Medical Waste", AccountNumber = "IM-33120", LeadTimeDays = 7,
                        PaymentTerms = "Monthly direct debit",
                        Contact = new ContactDetails { HomePhone = "0870 850 4045", Email = "service@example-im.co.uk", PreferredContactMethod = "Email" },
                        Address = new Address { Line1 = "Sterling House", City = "Reading", PostCode = "RG1 8LS" } }
            };

            db.Suppliers.AddRange(suppliers);
            await db.SaveChangesAsync(ct);

            var schein = suppliers[0];
            var directory = suppliers[1];
            var nobel = suppliers[2];

            var items = new List<InventoryItem>
            {
                Item("CON-001", "Nitrile examination gloves, medium", InventoryCategory.Ppe, "box", 12, 4, 10, 6.80m, schein.Id),
                Item("CON-002", "Nitrile examination gloves, small", InventoryCategory.Ppe, "box", 9, 4, 10, 6.80m, schein.Id),
                Item("CON-003", "Type IIR surgical face masks", InventoryCategory.Ppe, "box", 18, 6, 12, 4.20m, schein.Id),
                Item("CON-004", "Disposable bibs", InventoryCategory.Consumable, "pack", 22, 8, 20, 9.50m, directory.Id),
                Item("CON-005", "Saliva ejectors", InventoryCategory.Consumable, "pack", 15, 5, 15, 3.75m, directory.Id),
                Item("CON-006", "Cotton wool rolls", InventoryCategory.Consumable, "box", 7, 4, 12, 5.10m, directory.Id),
                Item("RES-001", "Composite A2 syringe", InventoryCategory.RestorativeMaterial, "syringe", 14, 6, 12, 28.40m, schein.Id, lot: true, expiry: true),
                Item("RES-002", "Composite A3 syringe", InventoryCategory.RestorativeMaterial, "syringe", 11, 6, 12, 28.40m, schein.Id, lot: true, expiry: true),
                Item("RES-003", "Glass ionomer cement kit", InventoryCategory.RestorativeMaterial, "kit", 5, 2, 4, 46.00m, schein.Id, lot: true, expiry: true),
                Item("RES-004", "Etchant gel 37% phosphoric acid", InventoryCategory.RestorativeMaterial, "syringe", 9, 4, 10, 7.20m, directory.Id, lot: true, expiry: true),
                Item("RES-005", "Universal bonding agent", InventoryCategory.RestorativeMaterial, "bottle", 4, 2, 5, 62.00m, schein.Id, lot: true, expiry: true),
                Item("ANA-001", "Lidocaine 2% with adrenaline 1:80,000", InventoryCategory.Anaesthetic, "box of 50", 6, 3, 6, 24.50m, schein.Id, lot: true, expiry: true),
                Item("ANA-002", "Articaine 4% with adrenaline 1:100,000", InventoryCategory.Anaesthetic, "box of 50", 8, 3, 6, 31.00m, schein.Id, lot: true, expiry: true),
                Item("ANA-003", "Dental needles 30G short", InventoryCategory.Anaesthetic, "box of 100", 5, 2, 5, 14.90m, directory.Id, lot: true, expiry: true),
                Item("IMP-001", "Implant NobelActive 4.3 x 10mm", InventoryCategory.ImplantComponent, "each", 6, 2, 4, 285.00m, nobel.Id, lot: true, expiry: true),
                Item("IMP-002", "Implant NobelActive 3.5 x 11.5mm", InventoryCategory.ImplantComponent, "each", 4, 2, 4, 285.00m, nobel.Id, lot: true, expiry: true),
                Item("IMP-003", "Healing abutment 4.3mm", InventoryCategory.ImplantComponent, "each", 9, 3, 6, 68.00m, nobel.Id, lot: true),
                Item("IMP-004", "Bio-Oss bone graft 0.5g", InventoryCategory.ImplantComponent, "vial", 5, 2, 4, 92.00m, nobel.Id, lot: true, expiry: true),
                Item("IMP-005", "Bio-Gide membrane 25 x 25mm", InventoryCategory.ImplantComponent, "each", 3, 2, 4, 118.00m, nobel.Id, lot: true, expiry: true),
                Item("SUR-001", "Vicryl 4-0 suture", InventoryCategory.Consumable, "box of 12", 4, 2, 4, 42.00m, schein.Id, lot: true, expiry: true),
                Item("SUR-002", "Surgical blades 15C", InventoryCategory.Consumable, "box of 100", 3, 1, 3, 22.00m, schein.Id, lot: true, expiry: true),
                Item("STE-001", "Sterilisation pouches 90 x 230mm", InventoryCategory.Sterilisation, "box of 200", 6, 3, 6, 18.50m, directory.Id),
                Item("STE-002", "Class 5 chemical indicators", InventoryCategory.Sterilisation, "box of 250", 4, 2, 4, 26.00m, directory.Id, lot: true, expiry: true),
                Item("STE-003", "Biological indicator vials", InventoryCategory.Sterilisation, "box of 25", 2, 1, 3, 48.00m, directory.Id, lot: true, expiry: true),
                Item("RAD-001", "Intraoral sensor sheaths", InventoryCategory.Radiography, "box of 500", 5, 2, 4, 32.00m, schein.Id),
                Item("IMS-001", "Alginate impression material", InventoryCategory.ImpressionMaterial, "bag", 7, 3, 6, 12.80m, directory.Id, lot: true, expiry: true),
                Item("IMS-002", "Polyvinyl siloxane light body", InventoryCategory.ImpressionMaterial, "cartridge", 10, 4, 8, 21.50m, schein.Id, lot: true, expiry: true)
            };

            db.InventoryItems.AddRange(items);
            await db.SaveChangesAsync(ct);

            // Opening lots for the tracked items
            var random = new Random(20260906);
            foreach (var item in items.Where(i => i.RequiresLotTracking))
            {
                var quantity = item.CurrentStock;
                var lot = new InventoryLot
                {
                    InventoryItemId = item.Id,
                    LotNumber = $"L{random.Next(100000, 999999)}",
                    ReceivedDate = new DateOnly(2026, 6, 1).AddDays(random.Next(0, 60)),
                    QuantityReceived = quantity,
                    QuantityRemaining = quantity,
                    UnitCost = item.UnitCost,
                    SupplierId = item.PreferredSupplierId,
                    ExpiryDate = item.RequiresExpiryTracking
                        ? new DateOnly(2026, 9, 1).AddDays(random.Next(20, 540))
                        : null
                };
                db.InventoryLots.Add(lot);

                db.StockMovements.Add(new StockMovement
                {
                    InventoryItemId = item.Id,
                    InventoryLotId = lot.Id,
                    MovementDateUtc = lot.ReceivedDate.ToDateTime(TimeOnly.MinValue),
                    MovementType = StockMovementType.Receipt,
                    Quantity = quantity,
                    BalanceAfter = quantity,
                    UnitCost = item.UnitCost,
                    Reason = "Opening stock"
                });
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} inventory items.", items.Count);
        }

        if (!await db.Sterilisers.AnyAsync(ct))
        {
            var sterilisers = new List<Steriliser>
            {
                new() { Name = "Autoclave 1 (Decontamination room)", Manufacturer = "W&H", Model = "Lisa VA5",
                        SerialNumber = "WH-LISA-88412", LocationId = main.Id,
                        InstallationDate = new DateOnly(2022, 3, 14),
                        LastServiceDate = new DateOnly(2026, 3, 10), NextServiceDue = new DateOnly(2026, 9, 10),
                        LastValidationDate = new DateOnly(2026, 3, 10), NextValidationDue = new DateOnly(2027, 3, 10),
                        PressureVesselCertExpiry = new DateOnly(2027, 3, 1), NextCycleNumber = 1 },
                new() { Name = "Autoclave 2 (Surgical)", Manufacturer = "Melag", Model = "Vacuklav 41B+",
                        SerialNumber = "ML-VK41-20977", LocationId = main.Id,
                        InstallationDate = new DateOnly(2023, 8, 2),
                        LastServiceDate = new DateOnly(2026, 2, 20), NextServiceDue = new DateOnly(2026, 8, 20),
                        LastValidationDate = new DateOnly(2026, 2, 20), NextValidationDue = new DateOnly(2027, 2, 20),
                        PressureVesselCertExpiry = new DateOnly(2027, 2, 15), NextCycleNumber = 1 }
            };

            db.Sterilisers.AddRange(sterilisers);

            var sets = new List<InstrumentSet>
            {
                Set("IS-EXAM-01", "Examination set A", 5, "Mirror, probe, tweezers, excavator, periodontal probe"),
                Set("IS-EXAM-02", "Examination set B", 5, "Mirror, probe, tweezers, excavator, periodontal probe"),
                Set("IS-EXAM-03", "Examination set C", 5, "Mirror, probe, tweezers, excavator, periodontal probe"),
                Set("IS-REST-01", "Restorative set A", 12, "Examination set, plastic instruments, matrix system, burnisher, flat plastic"),
                Set("IS-REST-02", "Restorative set B", 12, "Examination set, plastic instruments, matrix system, burnisher, flat plastic"),
                Set("IS-EXTR-01", "Extraction set A", 14, "Elevators (Coupland 1-3), Warwick James, upper and lower forceps", surgical: true),
                Set("IS-EXTR-02", "Extraction set B", 14, "Elevators (Coupland 1-3), Warwick James, upper and lower forceps", surgical: true),
                Set("IS-SURG-01", "Oral surgery set", 22, "Periosteal elevators, retractors, rongeurs, bone file, needle holder, scissors, suture forceps", surgical: true),
                Set("IS-IMPL-01", "Implant surgical kit", 28, "Drill kit, ratchet, torque wrench, depth gauges, tissue punch, osteotomes", surgical: true),
                Set("IS-PERIO-01", "Periodontal set", 16, "Gracey curettes 1/2 to 13/14, universal curette, scalers, periodontal probe", surgical: true),
                Set("IS-ENDO-01", "Endodontic set", 18, "Rubber dam kit, endo ruler, spreaders, pluggers, apex locator probe"),
                Set("IS-HYG-01", "Hygiene set A", 8, "Ultrasonic tips, Gracey curettes, mirror, probe, tweezers")
            };

            db.InstrumentSets.AddRange(sets);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} sterilisers and {Sets} instrument sets.",
                sterilisers.Count, sets.Count);
        }

        if (!await db.DentalLaboratories.AnyAsync(ct))
        {
            db.DentalLaboratories.AddRange(
                new DentalLaboratory
                {
                    Name = "Kingsway Dental Laboratory", AccountNumber = "KDL-1147",
                    ContactPerson = "Martin Whitcombe", StandardTurnaroundDays = 10,
                    AcceptsDigitalImpressions = true, IsPreferred = true,
                    Specialities = "Crown and bridge, all-ceramic, implant restorations",
                    Contact = new ContactDetails { HomePhone = "020 7946 1188", Email = "cases@example-kingsway.co.uk", PreferredContactMethod = "Email" },
                    Address = new Address { Line1 = "Unit 12 Kingsway Estate", City = "London", PostCode = "N7 9DP" }
                },
                new DentalLaboratory
                {
                    Name = "Aurelia Prosthetics", AccountNumber = "AP-2290",
                    ContactPerson = "Sofia Marchetti", StandardTurnaroundDays = 14,
                    AcceptsDigitalImpressions = true,
                    Specialities = "Removable prosthodontics, cobalt-chrome frameworks, implant overdentures",
                    Contact = new ContactDetails { HomePhone = "0161 496 0223", Email = "work@example-aurelia.co.uk", PreferredContactMethod = "Email" },
                    Address = new Address { Line1 = "5 Foundry Lane", City = "Manchester", PostCode = "M4 6BU" }
                },
                new DentalLaboratory
                {
                    Name = "ClearPath Orthodontic Lab", AccountNumber = "CP-0871",
                    ContactPerson = "Owen Pritchard", StandardTurnaroundDays = 21,
                    AcceptsDigitalImpressions = true,
                    Specialities = "Clear aligners, retainers, functional appliances",
                    Contact = new ContactDetails { HomePhone = "0117 325 4410", Email = "lab@example-clearpath.co.uk", PreferredContactMethod = "Email" },
                    Address = new Address { Line1 = "Aztec West", City = "Bristol", PostCode = "BS32 4AQ" }
                });

            await db.SaveChangesAsync(ct);
        }

        if (!await db.InsuranceCarriers.AnyAsync(ct))
        {
            var denplan = new InsuranceCarrier
            {
                Name = "Denplan", PayerId = "DEN-UK", ElectronicPayerId = "DENUK01", TypicalPaymentDays = 21,
                Contact = new ContactDetails { HomePhone = "0800 401 402", Email = "claims@example-denplan.co.uk", PreferredContactMethod = "Email" },
                Address = new Address { Line1 = "Denplan Court, Victoria Road", City = "Winchester", PostCode = "SO23 7RG" }
            };

            var bupa = new InsuranceCarrier
            {
                Name = "Bupa Dental Insurance", PayerId = "BUPA-DEN", ElectronicPayerId = "BUPADEN1", TypicalPaymentDays = 28,
                Contact = new ContactDetails { HomePhone = "0345 606 6482", Email = "dental.claims@example-bupa.co.uk", PreferredContactMethod = "Email" },
                Address = new Address { Line1 = "Bupa House, 15-19 Bloomsbury Way", City = "London", PostCode = "WC1A 2BA" }
            };

            var simplyhealth = new InsuranceCarrier
            {
                Name = "Simplyhealth", PayerId = "SH-DEN", ElectronicPayerId = "SHDEN001", TypicalPaymentDays = 14,
                Contact = new ContactDetails { HomePhone = "0370 908 3481", Email = "claims@example-simplyhealth.co.uk", PreferredContactMethod = "Email" },
                Address = new Address { Line1 = "Hambleden House, Waterloo Court", City = "Andover", PostCode = "SP10 1LQ" }
            };

            db.InsuranceCarriers.AddRange(denplan, bupa, simplyhealth);
            await db.SaveChangesAsync(ct);

            db.InsurancePlans.AddRange(
                new InsurancePlan
                {
                    InsuranceCarrierId = denplan.Id, PlanName = "Denplan Care", PlanType = InsurancePlanType.CapitationPlan,
                    AnnualMaximum = 2000m, PreventiveCoveragePercent = 100m, DiagnosticCoveragePercent = 100m,
                    BasicCoveragePercent = 90m, MajorCoveragePercent = 60m, ImplantCoveragePercent = 0m,
                    OrthodonticCoveragePercent = 0m, SurgeryCoveragePercent = 60m,
                    CoverageNotes = "Capitation plan covering routine and restorative care; laboratory work charged separately.",
                    Exclusions = "Implants, orthodontics and cosmetic treatment."
                },
                new InsurancePlan
                {
                    InsuranceCarrierId = bupa.Id, PlanName = "Bupa Dental Level 3", PlanType = InsurancePlanType.Ppo,
                    AnnualMaximum = 1500m, IndividualDeductible = 50m, PreventiveCoveragePercent = 100m,
                    DiagnosticCoveragePercent = 100m, BasicCoveragePercent = 80m, MajorCoveragePercent = 50m,
                    EndodonticCoveragePercent = 80m, PeriodonticCoveragePercent = 80m,
                    ImplantCoveragePercent = 25m, OrthodonticCoveragePercent = 50m,
                    OrthodonticLifetimeMaximum = 1000m, SurgeryCoveragePercent = 60m,
                    WaitingPeriodMajorMonths = 6, WaitingPeriodOrthoMonths = 12,
                    RequiresPreAuthorisation = true, PreAuthorisationThreshold = 500m
                },
                new InsurancePlan
                {
                    InsuranceCarrierId = simplyhealth.Id, PlanName = "Simplyhealth Dental Plan 2",
                    PlanType = InsurancePlanType.DiscountPlan, AnnualMaximum = 750m,
                    PreventiveCoveragePercent = 100m, DiagnosticCoveragePercent = 100m,
                    BasicCoveragePercent = 75m, MajorCoveragePercent = 40m,
                    ImplantCoveragePercent = 0m, OrthodonticCoveragePercent = 0m,
                    CoverageNotes = "Cash plan reimbursing a proportion of costs up to the annual limit."
                });

            await db.SaveChangesAsync(ct);
        }

        if (!await db.Pharmacies.AnyAsync(ct))
        {
            db.Pharmacies.AddRange(
                new Pharmacy
                {
                    Name = "Marylebone Pharmacy", PharmacyCode = "FQ421",
                    AcceptsElectronicPrescriptions = true,
                    Contact = new ContactDetails { HomePhone = "020 7935 2891", Email = "rx@example-marylebone.co.uk", PreferredContactMethod = "Phone" },
                    Address = new Address { Line1 = "88 Marylebone High Street", City = "London", PostCode = "W1U 4QY" }
                },
                new Pharmacy
                {
                    Name = "Riverside Chemist", PharmacyCode = "FT908",
                    AcceptsElectronicPrescriptions = true,
                    Contact = new ContactDetails { HomePhone = "020 7407 3312", Email = "dispensary@example-riverside.co.uk", PreferredContactMethod = "Phone" },
                    Address = new Address { Line1 = "14 Wharf Road", City = "London", PostCode = "SE1 2NB" }
                });

            await db.SaveChangesAsync(ct);
        }
    }

    private static InventoryItem Item(
        string sku, string name, InventoryCategory category, string unit,
        decimal stock, decimal reorderLevel, decimal reorderQuantity, decimal cost,
        Guid supplierId, bool lot = false, bool expiry = false) => new()
    {
        Sku = sku, Name = name, Category = category, UnitOfMeasure = unit,
        CurrentStock = stock, ReorderLevel = reorderLevel, ReorderQuantity = reorderQuantity,
        UnitCost = cost, LastPurchasePrice = cost, PreferredSupplierId = supplierId,
        RequiresLotTracking = lot, RequiresExpiryTracking = expiry,
        StorageLocation = category switch
        {
            InventoryCategory.ImplantComponent => "Implant cupboard (locked)",
            InventoryCategory.Anaesthetic => "Drug cupboard",
            InventoryCategory.Sterilisation => "Decontamination room",
            _ => "Main store"
        }
    };

    private static InstrumentSet Set(string code, string name, int itemCount, string contents, bool surgical = false) => new()
    {
        SetCode = code, Name = name, ItemCount = itemCount, Contents = contents,
        IsSurgicalSet = surgical, Status = InstrumentSetStatus.Sterile,
        SterilisedOn = new DateOnly(2026, 9, 1),
        SterilityExpiryDate = new DateOnly(2027, 9, 1),
        TrayType = surgical ? "Surgical cassette" : "Standard cassette"
    };
}
