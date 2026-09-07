using DentalSurgery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalSurgery.Infrastructure.Persistence.Configurations;

// ------------------------------------------------------------------ procedures

public class ProcedureCodeConfiguration : IEntityTypeConfiguration<ProcedureCode>
{
    public void Configure(EntityTypeBuilder<ProcedureCode> b)
    {
        b.ToTable("ProcedureCodes");
        b.Property(p => p.Code).HasMaxLength(20).IsRequired();
        b.Property(p => p.ShortDescription).HasMaxLength(200).IsRequired();
        b.Property(p => p.LongDescription).HasMaxLength(2000);
        b.HasIndex(p => p.Code).IsUnique();
        b.HasIndex(p => new { p.Category, p.IsActive });
    }
}

public class FeeScheduleConfiguration : IEntityTypeConfiguration<FeeSchedule>
{
    public void Configure(EntityTypeBuilder<FeeSchedule> b)
    {
        b.ToTable("FeeSchedules");
        b.Property(f => f.Name).HasMaxLength(160).IsRequired();
        b.HasMany(f => f.Items).WithOne(i => i.FeeSchedule!)
            .HasForeignKey(i => i.FeeScheduleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(f => f.InsuranceCarrier).WithMany()
            .HasForeignKey(f => f.InsuranceCarrierId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class FeeScheduleItemConfiguration : IEntityTypeConfiguration<FeeScheduleItem>
{
    public void Configure(EntityTypeBuilder<FeeScheduleItem> b)
    {
        b.ToTable("FeeScheduleItems");
        b.HasIndex(i => new { i.FeeScheduleId, i.ProcedureCodeId }).IsUnique();
        b.HasOne(i => i.ProcedureCode).WithMany(p => p.FeeScheduleItems)
            .HasForeignKey(i => i.ProcedureCodeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> b)
    {
        b.ToTable("TreatmentPlans");

        // A convenience projection over the phases, not a navigation.
        b.Ignore(p => p.AllItems);

        b.Property(p => p.PlanNumber).HasMaxLength(32).IsRequired();
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(p => p.PlanNumber).IsUnique();
        b.HasIndex(p => new { p.PatientId, p.Status });
        b.HasOne(p => p.Patient).WithMany(x => x.TreatmentPlans)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Phases).WithOne(ph => ph.TreatmentPlan!)
            .HasForeignKey(ph => ph.TreatmentPlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.Provider).WithMany()
            .HasForeignKey(p => p.ProviderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.FeeSchedule).WithMany()
            .HasForeignKey(p => p.FeeScheduleId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class TreatmentPlanPhaseConfiguration : IEntityTypeConfiguration<TreatmentPlanPhase>
{
    public void Configure(EntityTypeBuilder<TreatmentPlanPhase> b)
    {
        b.ToTable("TreatmentPlanPhases");
        b.Property(p => p.Name).HasMaxLength(160).IsRequired();
        b.HasMany(p => p.Items).WithOne(i => i.TreatmentPlanPhase!)
            .HasForeignKey(i => i.TreatmentPlanPhaseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TreatmentPlanItemConfiguration : IEntityTypeConfiguration<TreatmentPlanItem>
{
    public void Configure(EntityTypeBuilder<TreatmentPlanItem> b)
    {
        b.ToTable("TreatmentPlanItems");
        b.HasIndex(i => i.Status);
        b.HasOne(i => i.ProcedureCode).WithMany()
            .HasForeignKey(i => i.ProcedureCodeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.Tooth).WithMany()
            .HasForeignKey(i => i.ToothId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(i => i.Provider).WithMany()
            .HasForeignKey(i => i.ProviderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProcedureConfiguration : IEntityTypeConfiguration<Procedure>
{
    public void Configure(EntityTypeBuilder<Procedure> b)
    {
        b.ToTable("Procedures");
        b.HasIndex(p => new { p.PatientId, p.DateOfService });
        b.HasIndex(p => new { p.Status, p.DateOfService });
        b.Property(p => p.Notes).HasMaxLength(4000);

        b.HasOne(p => p.Patient).WithMany(x => x.Procedures)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.ProcedureCode).WithMany()
            .HasForeignKey(p => p.ProcedureCodeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Appointment).WithMany(a => a.CompletedProcedures)
            .HasForeignKey(p => p.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.TreatmentPlanItem).WithMany()
            .HasForeignKey(p => p.TreatmentPlanItemId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Tooth).WithMany()
            .HasForeignKey(p => p.ToothId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Assistant).WithMany()
            .HasForeignKey(p => p.AssistantId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Location).WithMany()
            .HasForeignKey(p => p.LocationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.InvoiceLine).WithMany()
            .HasForeignKey(p => p.InvoiceLineId).OnDelete(DeleteBehavior.SetNull);

        b.HasOne(p => p.SurgicalRecord).WithOne(s => s.Procedure!)
            .HasForeignKey<SurgicalRecord>(s => s.ProcedureId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.AnaesthesiaRecord).WithOne(a => a.Procedure!)
            .HasForeignKey<AnaesthesiaRecord>(a => a.ProcedureId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.MaterialsUsed).WithOne(m => m.Procedure!)
            .HasForeignKey(m => m.ProcedureId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SurgicalRecordConfiguration : IEntityTypeConfiguration<SurgicalRecord>
{
    public void Configure(EntityTypeBuilder<SurgicalRecord> b)
    {
        b.ToTable("SurgicalRecords");
        b.HasIndex(s => s.ProcedureId).IsUnique();
        b.Property(s => s.OperativeNote).HasMaxLength(8000);
        b.HasOne(s => s.InstrumentSet).WithMany()
            .HasForeignKey(s => s.InstrumentSetId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AnaesthesiaRecordConfiguration : IEntityTypeConfiguration<AnaesthesiaRecord>
{
    public void Configure(EntityTypeBuilder<AnaesthesiaRecord> b)
    {
        b.ToTable("AnaesthesiaRecords");
        b.HasIndex(a => a.ProcedureId).IsUnique();
        b.HasMany(a => a.Doses).WithOne(d => d.AnaesthesiaRecord!)
            .HasForeignKey(d => d.AnaesthesiaRecordId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.AdministeredByStaff).WithMany()
            .HasForeignKey(a => a.AdministeredByStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AnaesthesiaAgentDoseConfiguration : IEntityTypeConfiguration<AnaesthesiaAgentDose>
{
    public void Configure(EntityTypeBuilder<AnaesthesiaAgentDose> b)
    {
        b.ToTable("AnaesthesiaAgentDoses");
        b.Property(d => d.AgentName).HasMaxLength(120).IsRequired();
    }
}

public class DentalImplantConfiguration : IEntityTypeConfiguration<DentalImplant>
{
    public void Configure(EntityTypeBuilder<DentalImplant> b)
    {
        b.ToTable("DentalImplants");
        b.Property(i => i.Manufacturer).HasMaxLength(120).IsRequired();
        b.Property(i => i.SystemName).HasMaxLength(120).IsRequired();
        b.Property(i => i.LotNumber).HasMaxLength(60);
        b.HasIndex(i => new { i.PatientId, i.Status });
        b.HasIndex(i => i.LotNumber);
        b.HasOne(i => i.Patient).WithMany(p => p.Implants)
            .HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(i => i.Tooth).WithMany()
            .HasForeignKey(i => i.ToothId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.PlacementProcedure).WithMany()
            .HasForeignKey(i => i.PlacementProcedureId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(i => i.SurgeonStaff).WithMany()
            .HasForeignKey(i => i.SurgeonStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PostOperativeInstructionConfiguration : IEntityTypeConfiguration<PostOperativeInstruction>
{
    public void Configure(EntityTypeBuilder<PostOperativeInstruction> b)
    {
        b.ToTable("PostOperativeInstructions");
        b.Property(p => p.Code).HasMaxLength(40).IsRequired();
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(p => p.Code).IsUnique();
    }
}

public class ProcedureMaterialUsageConfiguration : IEntityTypeConfiguration<ProcedureMaterialUsage>
{
    public void Configure(EntityTypeBuilder<ProcedureMaterialUsage> b)
    {
        b.ToTable("ProcedureMaterialUsages");
        b.HasOne(m => m.InventoryItem).WithMany()
            .HasForeignKey(m => m.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.InventoryLot).WithMany()
            .HasForeignKey(m => m.InventoryLotId).OnDelete(DeleteBehavior.SetNull);
    }
}

// ------------------------------------------------------------------ scheduling

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.ToTable("Appointments");
        b.Property(a => a.AppointmentNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(a => a.AppointmentNumber).IsUnique();
        b.HasIndex(a => a.StartUtc);
        b.HasIndex(a => new { a.ProviderId, a.StartUtc });
        b.HasIndex(a => new { a.OperatoryId, a.StartUtc });
        b.HasIndex(a => new { a.PatientId, a.StartUtc });
        b.HasIndex(a => a.Status);
        b.Property(a => a.Reason).HasMaxLength(500);
        b.Property(a => a.Notes).HasMaxLength(2000);

        b.HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.Assistant).WithMany()
            .HasForeignKey(a => a.AssistantId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(a => a.Location).WithMany()
            .HasForeignKey(a => a.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Operatory).WithMany()
            .HasForeignKey(a => a.OperatoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(a => a.PlannedProcedures).WithOne(p => p.Appointment!)
            .HasForeignKey(p => p.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(a => a.Reminders).WithOne(r => r.Appointment!)
            .HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AppointmentProcedureConfiguration : IEntityTypeConfiguration<AppointmentProcedure>
{
    public void Configure(EntityTypeBuilder<AppointmentProcedure> b)
    {
        b.ToTable("AppointmentProcedures");
        b.HasOne(p => p.ProcedureCode).WithMany()
            .HasForeignKey(p => p.ProcedureCodeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.TreatmentPlanItem).WithMany()
            .HasForeignKey(p => p.TreatmentPlanItemId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Tooth).WithMany()
            .HasForeignKey(p => p.ToothId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AppointmentReminderConfiguration : IEntityTypeConfiguration<AppointmentReminder>
{
    public void Configure(EntityTypeBuilder<AppointmentReminder> b)
    {
        b.ToTable("AppointmentReminders");
        b.HasIndex(r => new { r.Status, r.ScheduledForUtc });
    }
}

public class RecallScheduleConfiguration : IEntityTypeConfiguration<RecallSchedule>
{
    public void Configure(EntityTypeBuilder<RecallSchedule> b)
    {
        b.ToTable("RecallSchedules");
        b.HasIndex(r => new { r.Status, r.DueDate });
        b.HasIndex(r => new { r.PatientId, r.RecallType });
        b.HasOne(r => r.Patient).WithMany(p => p.Recalls)
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.PreferredProvider).WithMany()
            .HasForeignKey(r => r.PreferredProviderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> b)
    {
        b.ToTable("WaitlistEntries");
        b.HasIndex(w => new { w.Status, w.Priority });
        b.HasOne(w => w.Patient).WithMany()
            .HasForeignKey(w => w.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(w => w.PreferredProvider).WithMany()
            .HasForeignKey(w => w.PreferredProviderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(w => w.Location).WithMany()
            .HasForeignKey(w => w.LocationId).OnDelete(DeleteBehavior.SetNull);
    }
}

// ------------------------------------------------------------------ billing

public class InsuranceCarrierConfiguration : IEntityTypeConfiguration<InsuranceCarrier>
{
    public void Configure(EntityTypeBuilder<InsuranceCarrier> b)
    {
        b.ToTable("InsuranceCarriers");
        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.OwnsOne(c => c.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(c => c.Address).IsRequired();
        b.OwnsOne(c => c.ClaimsAddress, a => OwnedTypeMapping.MapOptionalAddress(a, "ClaimsAddress"));
        b.OwnsOne(c => c.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(c => c.Contact).IsRequired();
        b.HasMany(c => c.Plans).WithOne(p => p.InsuranceCarrier!)
            .HasForeignKey(p => p.InsuranceCarrierId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InsurancePlanConfiguration : IEntityTypeConfiguration<InsurancePlan>
{
    public void Configure(EntityTypeBuilder<InsurancePlan> b)
    {
        b.ToTable("InsurancePlans");
        b.Property(p => p.PlanName).HasMaxLength(200).IsRequired();
        b.HasOne(p => p.FeeSchedule).WithMany()
            .HasForeignKey(p => p.FeeScheduleId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PatientInsuranceConfiguration : IEntityTypeConfiguration<PatientInsurance>
{
    public void Configure(EntityTypeBuilder<PatientInsurance> b)
    {
        b.ToTable("PatientInsurances");
        b.HasIndex(p => new { p.PatientId, p.Priority });
        b.OwnsOne(p => p.SubscriberName, n => OwnedTypeMapping.MapOptionalName(n, "Subscriber"));
        b.HasOne(p => p.Patient).WithMany(x => x.InsurancePolicies)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.InsurancePlan).WithMany()
            .HasForeignKey(p => p.InsurancePlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(p => p.Claims).WithOne(c => c.PatientInsurance!)
            .HasForeignKey(c => c.PatientInsuranceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.Property(i => i.InvoiceNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(i => i.InvoiceNumber).IsUnique();
        b.HasIndex(i => new { i.Status, i.DueDate });
        b.HasIndex(i => new { i.PatientId, i.IssueDate });

        b.HasMany(i => i.Lines).WithOne(l => l.Invoice!)
            .HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(i => i.PaymentAllocations).WithOne(a => a.Invoice!)
            .HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.GuarantorPatient).WithMany()
            .HasForeignKey(i => i.GuarantorPatientId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(i => i.Provider).WithMany()
            .HasForeignKey(i => i.ProviderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(i => i.Location).WithMany()
            .HasForeignKey(i => i.LocationId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("InvoiceLines");
        b.Property(l => l.Description).HasMaxLength(400).IsRequired();
        b.HasOne(l => l.ProcedureCode).WithMany()
            .HasForeignKey(l => l.ProcedureCodeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(l => l.Tooth).WithMany()
            .HasForeignKey(l => l.ToothId).OnDelete(DeleteBehavior.SetNull);
        b.Ignore(l => l.TaxAmount);
        b.Ignore(l => l.LineTotal);
        b.Ignore(l => l.Gross);
        b.Ignore(l => l.NetBeforeTax);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.Property(p => p.PaymentNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(p => p.PaymentNumber).IsUnique();
        b.HasIndex(p => new { p.PatientId, p.PaymentDate });
        b.HasOne(p => p.Patient).WithMany(x => x.Payments)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Allocations).WithOne(a => a.Payment!)
            .HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.InsuranceClaim).WithMany()
            .HasForeignKey(p => p.InsuranceClaimId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> b)
    {
        b.ToTable("PaymentAllocations");
        b.HasIndex(a => new { a.PaymentId, a.InvoiceId });
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.ToTable("LedgerEntries");
        b.Property(l => l.Description).HasMaxLength(400).IsRequired();
        b.HasIndex(l => new { l.PatientId, l.EntryDate });
        b.HasOne(l => l.Patient).WithMany(p => p.LedgerEntries)
            .HasForeignKey(l => l.PatientId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AccountAdjustmentConfiguration : IEntityTypeConfiguration<AccountAdjustment>
{
    public void Configure(EntityTypeBuilder<AccountAdjustment> b)
    {
        b.ToTable("AccountAdjustments");
        b.Property(a => a.Reason).HasMaxLength(400).IsRequired();
        b.HasOne(a => a.Patient).WithMany()
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> b)
    {
        b.ToTable("PaymentPlans");
        b.Property(p => p.PlanNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(p => p.PlanNumber).IsUnique();
        b.HasOne(p => p.Patient).WithMany()
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Installments).WithOne(i => i.PaymentPlan!)
            .HasForeignKey(i => i.PaymentPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentPlanInstallmentConfiguration : IEntityTypeConfiguration<PaymentPlanInstallment>
{
    public void Configure(EntityTypeBuilder<PaymentPlanInstallment> b)
    {
        b.ToTable("PaymentPlanInstallments");
        b.HasIndex(i => new { i.PaymentPlanId, i.InstallmentNumber }).IsUnique();
        b.HasIndex(i => new { i.Status, i.DueDate });
    }
}

public class InsuranceClaimConfiguration : IEntityTypeConfiguration<InsuranceClaim>
{
    public void Configure(EntityTypeBuilder<InsuranceClaim> b)
    {
        b.ToTable("InsuranceClaims");
        b.Property(c => c.ClaimNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(c => c.ClaimNumber).IsUnique();
        b.HasIndex(c => new { c.Status, c.SubmittedOn });
        b.HasOne(c => c.Patient).WithMany()
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Invoice).WithMany()
            .HasForeignKey(c => c.InvoiceId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(c => c.Provider).WithMany()
            .HasForeignKey(c => c.ProviderId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(c => c.Lines).WithOne(l => l.InsuranceClaim!)
            .HasForeignKey(l => l.InsuranceClaimId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InsuranceClaimLineConfiguration : IEntityTypeConfiguration<InsuranceClaimLine>
{
    public void Configure(EntityTypeBuilder<InsuranceClaimLine> b)
    {
        b.ToTable("InsuranceClaimLines");
        b.HasOne(l => l.Procedure).WithMany()
            .HasForeignKey(l => l.ProcedureId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(l => l.ProcedureCode).WithMany()
            .HasForeignKey(l => l.ProcedureCodeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(l => l.Tooth).WithMany()
            .HasForeignKey(l => l.ToothId).OnDelete(DeleteBehavior.SetNull);
    }
}

// ------------------------------------------------------------------ inventory & operations

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("Suppliers");
        b.Property(s => s.Name).HasMaxLength(200).IsRequired();
        b.OwnsOne(s => s.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(s => s.Address).IsRequired();
        b.OwnsOne(s => s.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(s => s.Contact).IsRequired();
        b.HasMany(s => s.Items).WithOne(i => i.PreferredSupplier!)
            .HasForeignKey(i => i.PreferredSupplierId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(s => s.PurchaseOrders).WithOne(p => p.Supplier!)
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("InventoryItems");
        b.Property(i => i.Sku).HasMaxLength(40).IsRequired();
        b.Property(i => i.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(i => i.Sku).IsUnique();
        b.HasIndex(i => new { i.Category, i.IsActive });
        b.HasMany(i => i.Lots).WithOne(l => l.InventoryItem!)
            .HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(i => i.Movements).WithOne(m => m.InventoryItem!)
            .HasForeignKey(m => m.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> b)
    {
        b.ToTable("InventoryLots");
        b.Property(l => l.LotNumber).HasMaxLength(60).IsRequired();
        b.HasIndex(l => new { l.InventoryItemId, l.LotNumber });
        b.HasIndex(l => l.ExpiryDate);
        b.HasOne(l => l.PurchaseOrder).WithMany()
            .HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements");
        b.HasIndex(m => new { m.InventoryItemId, m.MovementDateUtc });
        b.HasOne(m => m.InventoryLot).WithMany()
            .HasForeignKey(m => m.InventoryLotId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("PurchaseOrders");
        b.Property(p => p.OrderNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(p => p.OrderNumber).IsUnique();
        b.HasMany(p => p.Lines).WithOne(l => l.PurchaseOrder!)
            .HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("PurchaseOrderLines");
        b.HasOne(l => l.InventoryItem).WithMany()
            .HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SteriliserConfiguration : IEntityTypeConfiguration<Steriliser>
{
    public void Configure(EntityTypeBuilder<Steriliser> b)
    {
        b.ToTable("Sterilisers");
        b.Property(s => s.Name).HasMaxLength(120).IsRequired();
        b.HasMany(s => s.Cycles).WithOne(c => c.Steriliser!)
            .HasForeignKey(c => c.SteriliserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(s => s.Location).WithMany()
            .HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SterilisationCycleConfiguration : IEntityTypeConfiguration<SterilisationCycle>
{
    public void Configure(EntityTypeBuilder<SterilisationCycle> b)
    {
        b.ToTable("SterilisationCycles");
        b.HasIndex(c => new { c.SteriliserId, c.CycleNumber }).IsUnique();
        b.HasIndex(c => c.StartedAtUtc);
        b.HasMany(c => c.Sets).WithOne(s => s.LastCycle!)
            .HasForeignKey(s => s.LastCycleId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(c => c.OperatorStaff).WithMany()
            .HasForeignKey(c => c.OperatorStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class InstrumentSetConfiguration : IEntityTypeConfiguration<InstrumentSet>
{
    public void Configure(EntityTypeBuilder<InstrumentSet> b)
    {
        b.ToTable("InstrumentSets");
        b.Property(s => s.SetCode).HasMaxLength(40).IsRequired();
        b.Property(s => s.Name).HasMaxLength(160).IsRequired();
        b.HasIndex(s => s.SetCode).IsUnique();
        b.HasMany(s => s.Usages).WithOne(u => u.InstrumentSet!)
            .HasForeignKey(u => u.InstrumentSetId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InstrumentSetUsageConfiguration : IEntityTypeConfiguration<InstrumentSetUsage>
{
    public void Configure(EntityTypeBuilder<InstrumentSetUsage> b)
    {
        b.ToTable("InstrumentSetUsages");
        b.HasIndex(u => u.UsedAtUtc);
    }
}

public class DentalLaboratoryConfiguration : IEntityTypeConfiguration<DentalLaboratory>
{
    public void Configure(EntityTypeBuilder<DentalLaboratory> b)
    {
        b.ToTable("DentalLaboratories");
        b.Property(l => l.Name).HasMaxLength(200).IsRequired();
        b.OwnsOne(l => l.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(l => l.Address).IsRequired();
        b.OwnsOne(l => l.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(l => l.Contact).IsRequired();
        b.HasMany(l => l.Cases).WithOne(c => c.DentalLaboratory!)
            .HasForeignKey(c => c.DentalLaboratoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LabCaseConfiguration : IEntityTypeConfiguration<LabCase>
{
    public void Configure(EntityTypeBuilder<LabCase> b)
    {
        b.ToTable("LabCases");
        b.Property(c => c.CaseNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(c => c.CaseNumber).IsUnique();
        b.HasIndex(c => new { c.Status, c.DueDate });
        b.HasOne(c => c.Patient).WithMany(p => p.LabCases)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Provider).WithMany()
            .HasForeignKey(c => c.ProviderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CommunicationLogConfiguration : IEntityTypeConfiguration<CommunicationLog>
{
    public void Configure(EntityTypeBuilder<CommunicationLog> b)
    {
        b.ToTable("CommunicationLogs");
        b.HasIndex(c => new { c.PatientId, c.OccurredAtUtc });
        b.Property(c => c.Subject).HasMaxLength(300);
        b.HasOne(c => c.Patient).WithMany(p => p.Communications)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Staff).WithMany()
            .HasForeignKey(c => c.StaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> b)
    {
        b.ToTable("MessageTemplates");
        b.Property(t => t.Code).HasMaxLength(40).IsRequired();
        b.Property(t => t.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(t => t.Code).IsUnique();
    }
}

// ------------------------------------------------------------------ system

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(a => a.Id);
        b.Property(a => a.EntityName).HasMaxLength(160).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(64);
        b.Property(a => a.UserId).HasMaxLength(450);
        b.Property(a => a.UserName).HasMaxLength(256);
        b.Property(a => a.IpAddress).HasMaxLength(64);
        b.HasIndex(a => a.TimestampUtc);
        b.HasIndex(a => new { a.EntityName, a.EntityId });
        b.HasIndex(a => a.PatientId);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.ToTable("AppSettings");
        b.Property(s => s.Key).HasMaxLength(160).IsRequired();
        b.HasIndex(s => s.Key).IsUnique();
    }
}

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> b)
    {
        b.ToTable("NumberSequences");
        b.Property(s => s.Name).HasMaxLength(80).IsRequired();
        b.Property(s => s.Prefix).HasMaxLength(16);
        b.HasIndex(s => s.Name).IsUnique();
    }
}

public class SavedViewConfiguration : IEntityTypeConfiguration<SavedView>
{
    public void Configure(EntityTypeBuilder<SavedView> b)
    {
        b.ToTable("SavedViews");
        b.Property(v => v.Name).HasMaxLength(160).IsRequired();
        b.Property(v => v.Module).HasMaxLength(80).IsRequired();
        b.HasIndex(v => new { v.Module, v.OwnerUserId });
    }
}

public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> b)
    {
        b.ToTable("WorkTasks");
        b.Property(t => t.Title).HasMaxLength(300).IsRequired();
        b.HasIndex(t => new { t.IsCompleted, t.DueDate });
        b.HasOne(t => t.Patient).WithMany()
            .HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(t => t.AssignedToStaff).WithMany()
            .HasForeignKey(t => t.AssignedToStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}
