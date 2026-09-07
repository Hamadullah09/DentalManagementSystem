using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Reflection;

namespace DentalSurgery.Infrastructure.Persistence;

/// <summary>
/// The single database context. It carries both the Identity tables and the
/// clinical/operational model so that staff logins and clinical attribution
/// share one transactional boundary.
/// </summary>
public class DentalDbContext(DbContextOptions<DentalDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options), IDataProtectionKeyContext
{
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
        ApplySoftDeleteFilters(builder);
        ApplyConcurrencyTokens(builder, IsSqlite);
        RestrictCascadeDeletes(builder);
    }

    /// <summary>Gives the Identity tables friendlier names alongside the domain tables.</summary>
    private static void ApplyIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
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

    /// <summary>Hides soft-deleted rows from every query by default.</summary>
    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (entity.IsOwned()) continue;
            if (!typeof(ISoftDeletable).IsAssignableFrom(entity.ClrType)) continue;

            var parameter = Expression.Parameter(entity.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);
            entity.SetQueryFilter(filter);
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
