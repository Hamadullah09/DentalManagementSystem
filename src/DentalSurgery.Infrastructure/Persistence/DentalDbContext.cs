using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Reflection;

namespace DentalSurgery.Infrastructure.Persistence;

/// <summary>
/// The single database context. It carries both the Identity tables and the
/// clinical/operational model so that staff logins and clinical attribution
/// share one transactional boundary.
/// </summary>
public class DentalDbContext(
    DbContextOptions<DentalDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options), IDataProtectionKeyContext
{
    /// <summary>
    /// Read by the global query filters. EF re-evaluates these on every query
    /// rather than baking them into the compiled plan, so one context can serve
    /// a request whose tenant is resolved after the context was created.
    /// </summary>
    private Guid? CurrentTenantId => tenantContext.TenantId;

    private bool IsPlatformScope => tenantContext.IsPlatformScope;

    /// <summary>The tenant registry. Global: it is the list of tenants.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    // -------------------------------------------------------- organisation
    public DbSet<Practice> Practices => Set<Practice>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Operatory> Operatories => Set<Operatory>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<ClinicClosure> ClinicClosures => Set<ClinicClosure>();

    // -------------------------------------------------------- people
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientContact> PatientContacts => Set<PatientContact>();
    public DbSet<PatientAlert> PatientAlerts => Set<PatientAlert>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<SocialHistory> SocialHistories => Set<SocialHistory>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffScheduleSlot> StaffScheduleSlots => Set<StaffScheduleSlot>();
    public DbSet<StaffTimeOff> StaffTimeOff => Set<StaffTimeOff>();

    // -------------------------------------------------------- medical history
    public DbSet<MedicalCondition> MedicalConditions => Set<MedicalCondition>();
    public DbSet<PatientMedicalCondition> PatientMedicalConditions => Set<PatientMedicalCondition>();
    public DbSet<Allergen> Allergens => Set<Allergen>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<MedicalHistoryReview> MedicalHistoryReviews => Set<MedicalHistoryReview>();
    public DbSet<VitalSignRecord> VitalSignRecords => Set<VitalSignRecord>();

    // -------------------------------------------------------- charting
    public DbSet<Tooth> Teeth => Set<Tooth>();
    public DbSet<ToothConditionRecord> ToothConditionRecords => Set<ToothConditionRecord>();
    public DbSet<PeriodontalChart> PeriodontalCharts => Set<PeriodontalChart>();
    public DbSet<PeriodontalMeasurement> PeriodontalMeasurements => Set<PeriodontalMeasurement>();

    // -------------------------------------------------------- clinical records
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<ClinicalNoteAddendum> ClinicalNoteAddenda => Set<ClinicalNoteAddendum>();
    public DbSet<PatientDiagnosis> PatientDiagnoses => Set<PatientDiagnosis>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<RadiographRecord> RadiographRecords => Set<RadiographRecord>();
    public DbSet<ConsentFormTemplate> ConsentFormTemplates => Set<ConsentFormTemplate>();
    public DbSet<PatientConsent> PatientConsents => Set<PatientConsent>();
    public DbSet<Referral> Referrals => Set<Referral>();

    // -------------------------------------------------------- procedures
    public DbSet<ProcedureCode> ProcedureCodes => Set<ProcedureCode>();
    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();
    public DbSet<FeeScheduleItem> FeeScheduleItems => Set<FeeScheduleItem>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanPhase> TreatmentPlanPhases => Set<TreatmentPlanPhase>();
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<ProcedureMaterialUsage> ProcedureMaterialUsages => Set<ProcedureMaterialUsage>();
    public DbSet<SurgicalRecord> SurgicalRecords => Set<SurgicalRecord>();
    public DbSet<AnaesthesiaRecord> AnaesthesiaRecords => Set<AnaesthesiaRecord>();
    public DbSet<AnaesthesiaAgentDose> AnaesthesiaAgentDoses => Set<AnaesthesiaAgentDose>();
    public DbSet<DentalImplant> DentalImplants => Set<DentalImplant>();
    public DbSet<PostOperativeInstruction> PostOperativeInstructions => Set<PostOperativeInstruction>();

    // -------------------------------------------------------- scheduling
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentProcedure> AppointmentProcedures => Set<AppointmentProcedure>();
    public DbSet<AppointmentReminder> AppointmentReminders => Set<AppointmentReminder>();
    public DbSet<RecallSchedule> RecallSchedules => Set<RecallSchedule>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    // -------------------------------------------------------- billing
    public DbSet<InsuranceCarrier> InsuranceCarriers => Set<InsuranceCarrier>();
    public DbSet<InsurancePlan> InsurancePlans => Set<InsurancePlan>();
    public DbSet<PatientInsurance> PatientInsurances => Set<PatientInsurance>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<AccountAdjustment> AccountAdjustments => Set<AccountAdjustment>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<PaymentPlanInstallment> PaymentPlanInstallments => Set<PaymentPlanInstallment>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
    public DbSet<InsuranceClaimLine> InsuranceClaimLines => Set<InsuranceClaimLine>();

    // -------------------------------------------------------- operations
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Steriliser> Sterilisers => Set<Steriliser>();
    public DbSet<SterilisationCycle> SterilisationCycles => Set<SterilisationCycle>();
    public DbSet<InstrumentSet> InstrumentSets => Set<InstrumentSet>();
    public DbSet<InstrumentSetUsage> InstrumentSetUsages => Set<InstrumentSetUsage>();
    public DbSet<DentalLaboratory> DentalLaboratories => Set<DentalLaboratory>();
    public DbSet<LabCase> LabCases => Set<LabCase>();
    public DbSet<CommunicationLog> CommunicationLogs => Set<CommunicationLog>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();

    // -------------------------------------------------------- system
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();

    /// <summary>
    /// Data protection keys live in the same database as everything else so
    /// that every instance of the application shares one key ring. Without this
    /// each host generates its own keys in its local profile, and a session
    /// established on one host is rejected by the next — which presents as
    /// users being signed out at random behind a load balancer.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>
    /// True when the store is SQLite, which lacks a decimal type and a native
    /// rowversion and therefore needs the workarounds below. Resolved from the
    /// configured provider rather than assumed, so one model definition serves
    /// SQLite for local work and SQL Server in production.
    /// </summary>
    private bool IsSqlite =>
        Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyIdentityTableNames(builder);
        ApplyDecimalPrecision(builder, IsSqlite);
        ApplyEnumConversions(builder);
        GuardEveryEntityIsClassified(builder);
        ApplyQueryFilters(builder);
        ApplyTenantIndexes(builder);
        ApplyConcurrencyTokens(builder, IsSqlite);
        RestrictCascadeDeletes(builder);
    }

    /// <summary>Gives the Identity tables friendlier names alongside the domain tables.</summary>
    private static void ApplyIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");

    }

    /// <summary>
    /// Money is <c>decimal(18,4)</c> wherever the database has a decimal type.
    /// <para>
    /// SQLite does not. Left alone, EF stores decimals there as TEXT, which
    /// makes SUM untranslatable and sorts money lexicographically ("9.00" above
    /// "10.00"). On SQLite only, decimals therefore go through a converter to
    /// REAL, rounded to four places on read: correct for aggregation and
    /// ordering, and exact at the magnitudes a practice ledger holds.
    /// </para>
    /// <para>
    /// On SQL Server the converter is omitted and the native decimal type is
    /// used, so money is stored exactly rather than in binary floating point.
    /// </para>
    /// </summary>
    private static void ApplyDecimalPrecision(ModelBuilder builder, bool isSqlite)
    {
        var converter = new ValueConverter<decimal, double>(
            value => (double)value,
            value => Math.Round((decimal)value, 4));

        var nullableConverter = new ValueConverter<decimal?, double?>(
            value => value.HasValue ? (double)value.Value : null,
            value => value.HasValue ? Math.Round((decimal)value.Value, 4) : null);

        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties()))
        {
            if (property.ClrType == typeof(decimal))
            {
                property.SetPrecision(18);
                property.SetScale(4);
                if (isSqlite) property.SetValueConverter(converter);
            }
            else if (property.ClrType == typeof(decimal?))
            {
                property.SetPrecision(18);
                property.SetScale(4);
                if (isSqlite) property.SetValueConverter(nullableConverter);
            }
        }
    }

    /// <summary>Stores enums as integers, which keeps indexes small and stable.</summary>
    private static void ApplyEnumConversions(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type.IsEnum) property.SetProviderClrType(typeof(int));
            }
        }
    }

    /// <summary>
    /// Refuses to build a model containing an entity nobody classified.
    /// <para>
    /// An entity that is neither tenant-scoped nor explicitly global would be
    /// unfiltered, and therefore readable by every tenant. Catching that when
    /// the model is built turns the most dangerous mistake in a multi-tenant
    /// system into a start-up failure naming the offending type, instead of a
    /// data leak nobody notices.
    /// </para>
    /// </summary>
    private static void GuardEveryEntityIsClassified(ModelBuilder builder)
    {
        var unclassified = builder.Model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .Select(e => e.ClrType)
            .Where(t => !typeof(ITenantScoped).IsAssignableFrom(t))
            .Where(t => !typeof(IGlobalEntity).IsAssignableFrom(t))
            .Where(t => !IsFrameworkOwned(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        if (unclassified.Count > 0)
        {
            throw new InvalidOperationException(
                $"These entity types are neither ITenantScoped nor IGlobalEntity, so they would be " +
                $"visible to every tenant: {string.Join(", ", unclassified)}. Derive from TenantEntity " +
                $"to place one inside the tenant boundary, or mark it IGlobalEntity if it really is " +
                $"reference data shared by every practice.");
        }
    }

    /// <summary>
    /// Types owned by ASP.NET Core rather than this domain. Identity's own
    /// tables are scoped through ApplicationUser, and the data protection key
    /// ring is platform infrastructure that holds no tenant data.
    /// </summary>
    private static bool IsFrameworkOwned(Type type) =>
        type == typeof(DataProtectionKey)
        || type.Namespace?.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal) == true;

    /// <summary>
    /// The two filters every read passes through: the tenant boundary, and soft
    /// deletion.
    /// <para>
    /// They are combined into one expression because EF Core allows a single
    /// filter per entity — defining them separately would silently discard the
    /// first, which is exactly the sort of quiet failure this code cannot
    /// afford.
    /// </para>
    /// <para>
    /// The tenant predicate reads context properties rather than a captured
    /// constant, so EF treats them as parameters and re-evaluates them per
    /// query. That is what lets a pooled or factory-created context serve a
    /// request whose tenant was resolved later.
    /// </para>
    /// </summary>
    private void ApplyQueryFilters(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.IsOwned()) continue;

            var clr = entity.ClrType;
            var scoped = typeof(ITenantScoped).IsAssignableFrom(clr);
            var deletable = typeof(ISoftDeletable).IsAssignableFrom(clr);

            if (!scoped && !deletable) continue;

            var parameter = Expression.Parameter(clr, "e");
            Expression? predicate = null;

            if (scoped)
            {
                // e.TenantId == CurrentTenantId, waived inside a platform scope.
                var tenantProperty = Expression.Property(parameter, nameof(ITenantScoped.TenantId));

                var currentTenant = Expression.Convert(
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantId)),
                    typeof(Guid?));

                var matchesTenant = Expression.Equal(
                    Expression.Convert(tenantProperty, typeof(Guid?)),
                    currentTenant);

                var platform = Expression.Property(Expression.Constant(this), nameof(IsPlatformScope));

                predicate = Expression.OrElse(platform, matchesTenant);
            }

            if (deletable)
            {
                var notDeleted = Expression.Not(
                    Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));

                predicate = predicate is null ? notDeleted : Expression.AndAlso(predicate, notDeleted);
            }

            entity.SetQueryFilter(Expression.Lambda(predicate!, parameter));
        }
    }

    /// <summary>
    /// Makes every index on a tenant-scoped table tenant-aware.
    /// <para>
    /// Two jobs, and the second is a correctness fix rather than a performance
    /// one. Every query now carries a tenant predicate, so <c>TenantId</c> is
    /// the most selective column in the system and belongs at the front of each
    /// index; without that a shared database degrades into full scans as
    /// tenants are added.
    /// </para>
    /// <para>
    /// More importantly, a <em>unique</em> index that does not mention the
    /// tenant is enforced across the whole platform. "Patient number P-000001
    /// is unique" then means unique across every practice, so the second
    /// practice to open cannot register its first patient — and an attacker can
    /// probe which identifiers another tenant holds by watching which inserts
    /// fail. Every unique index on a scoped entity is therefore rewritten to
    /// lead with TenantId, which scopes the constraint to one practice.
    /// </para>
    /// <para>
    /// Applied as a convention rather than by editing each configuration, so an
    /// index added later cannot forget.
    /// </para>
    /// </summary>
    private static void ApplyTenantIndexes(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.IsOwned()) continue;
            if (!typeof(ITenantScoped).IsAssignableFrom(entity.ClrType)) continue;

            var tenantProperty = entity.FindProperty(nameof(ITenantScoped.TenantId));
            if (tenantProperty is null) continue;

            tenantProperty.IsNullable = false;

            foreach (var index in entity.GetIndexes().ToList())
            {
                if (index.Properties.Contains(tenantProperty)) continue;

                if (!index.IsUnique)
                {
                    // Non-unique indexes are only a performance concern; leave
                    // them and add the tenant lookup separately below.
                    continue;
                }

                var name = index.GetDatabaseName();
                var properties = new List<IMutableProperty> { tenantProperty };
                properties.AddRange(index.Properties.Cast<IMutableProperty>());

                entity.RemoveIndex(index.Properties);

                var replacement = entity.AddIndex(properties);
                replacement.IsUnique = true;
                if (!string.IsNullOrEmpty(name)) replacement.SetDatabaseName(name);
            }

            // A plain tenant lookup, for the many queries that filter on nothing else.
            if (entity.FindIndex(tenantProperty) is null)
                entity.AddIndex(tenantProperty);
        }
    }

    /// <summary>
    /// Optimistic concurrency on every aggregate root.
    /// <para>
    /// On SQL Server this is a real <c>rowversion</c>: the database stamps it
    /// atomically on write, so two hosts updating the same row cannot both
    /// believe they won. On SQLite, which has no such type, the token is a byte
    /// array that <c>AuditingInterceptor</c> rewrites on each save — weaker,
    /// because the read-modify-write is not atomic, but sufficient for the
    /// single-process development use SQLite is kept for.
    /// </para>
    /// </summary>
    private static void ApplyConcurrencyTokens(ModelBuilder builder, bool isSqlite)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.IsOwned()) continue;
            if (!typeof(BaseEntity).IsAssignableFrom(entity.ClrType)) continue;

            var property = entity.FindProperty(nameof(BaseEntity.RowVersion));
            if (property is null) continue;

            property.IsConcurrencyToken = true;

            if (isSqlite)
            {
                property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
            else
            {
                // Database-generated. The interceptor detects this and leaves
                // the column alone; writing to it would be rejected.
                property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
                property.SetColumnType("rowversion");
                property.IsNullable = false;
            }
        }
    }

    /// <summary>
    /// Clinical history must not disappear because a parent row was removed,
    /// so cascade delete is turned off wherever it is not an owned aggregate.
    /// </summary>
    private static void RestrictCascadeDeletes(ModelBuilder builder)
    {
        foreach (var relationship in builder.Model.GetEntityTypes()
                     .Where(t => !t.IsOwned())
                     .SelectMany(t => t.GetForeignKeys())
                     .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
