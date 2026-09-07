using DentalSurgery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalSurgery.Infrastructure.Persistence.Configurations;

public class PracticeConfiguration : IEntityTypeConfiguration<Practice>
{
    public void Configure(EntityTypeBuilder<Practice> b)
    {
        b.ToTable("Practices");
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.CurrencyCode).HasMaxLength(3);
        b.Property(p => p.CurrencySymbol).HasMaxLength(5);
        b.OwnsOne(p => p.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(p => p.Address).IsRequired();
        b.OwnsOne(p => p.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(p => p.Contact).IsRequired();
        b.HasMany(p => p.Locations).WithOne(l => l.Practice!)
            .HasForeignKey(l => l.PracticeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> b)
    {
        b.ToTable("Locations");
        b.Property(l => l.Code).HasMaxLength(20).IsRequired();
        b.Property(l => l.Name).HasMaxLength(160).IsRequired();
        b.HasIndex(l => l.Code).IsUnique();
        b.OwnsOne(l => l.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(l => l.Address).IsRequired();
        b.OwnsOne(l => l.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(l => l.Contact).IsRequired();
        b.HasMany(l => l.Operatories).WithOne(o => o.Location!)
            .HasForeignKey(o => o.LocationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(l => l.BusinessHours).WithOne(h => h.Location!)
            .HasForeignKey(h => h.LocationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(l => l.Closures).WithOne(c => c.Location!)
            .HasForeignKey(c => c.LocationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OperatoryConfiguration : IEntityTypeConfiguration<Operatory>
{
    public void Configure(EntityTypeBuilder<Operatory> b)
    {
        b.ToTable("Operatories");
        b.Property(o => o.Code).HasMaxLength(20).IsRequired();
        b.Property(o => o.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(o => new { o.LocationId, o.Code }).IsUnique();
    }
}

public class ToothConfiguration : IEntityTypeConfiguration<Tooth>
{
    public void Configure(EntityTypeBuilder<Tooth> b)
    {
        b.ToTable("Teeth");
        b.Property(t => t.UniversalNumber).HasMaxLength(4).IsRequired();
        b.Property(t => t.PalmerNotation).HasMaxLength(8).IsRequired();
        b.Property(t => t.Name).HasMaxLength(80).IsRequired();
        b.Property(t => t.ShortName).HasMaxLength(24).IsRequired();
        b.HasIndex(t => t.FdiNumber).IsUnique();
        b.HasIndex(t => t.ChartOrder);
    }
}

public class ToothConditionRecordConfiguration : IEntityTypeConfiguration<ToothConditionRecord>
{
    public void Configure(EntityTypeBuilder<ToothConditionRecord> b)
    {
        b.ToTable("ToothConditionRecords");
        b.HasIndex(r => new { r.PatientId, r.ToothId });
        b.HasIndex(r => new { r.PatientId, r.Status });
        b.Property(r => r.Notes).HasMaxLength(2000);

        b.HasOne(r => r.Patient).WithMany(p => p.ToothConditions)
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.Tooth).WithMany(t => t.Conditions)
            .HasForeignKey(r => r.ToothId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.Supersedes).WithMany()
            .HasForeignKey(r => r.SupersedesId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(r => r.Procedure).WithMany()
            .HasForeignKey(r => r.ProcedureId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(r => r.RecordedByStaff).WithMany()
            .HasForeignKey(r => r.RecordedByStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PeriodontalChartConfiguration : IEntityTypeConfiguration<PeriodontalChart>
{
    public void Configure(EntityTypeBuilder<PeriodontalChart> b)
    {
        b.ToTable("PeriodontalCharts");
        b.HasIndex(c => new { c.PatientId, c.ExamDate });
        b.Property(c => c.BpeUpperRight).HasMaxLength(3);
        b.Property(c => c.BpeUpperAnterior).HasMaxLength(3);
        b.Property(c => c.BpeUpperLeft).HasMaxLength(3);
        b.Property(c => c.BpeLowerRight).HasMaxLength(3);
        b.Property(c => c.BpeLowerAnterior).HasMaxLength(3);
        b.Property(c => c.BpeLowerLeft).HasMaxLength(3);

        b.HasOne(c => c.Patient).WithMany(p => p.PeriodontalCharts)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(c => c.Measurements).WithOne(m => m.PeriodontalChart!)
            .HasForeignKey(m => m.PeriodontalChartId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.ExaminerStaff).WithMany()
            .HasForeignKey(c => c.ExaminerStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PeriodontalMeasurementConfiguration : IEntityTypeConfiguration<PeriodontalMeasurement>
{
    public void Configure(EntityTypeBuilder<PeriodontalMeasurement> b)
    {
        b.ToTable("PeriodontalMeasurements");
        b.HasIndex(m => new { m.PeriodontalChartId, m.ToothId, m.Site }).IsUnique();
        b.HasOne(m => m.Tooth).WithMany()
            .HasForeignKey(m => m.ToothId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> b)
    {
        b.ToTable("ClinicalNotes");
        b.HasIndex(n => new { n.PatientId, n.NoteDateUtc });
        b.Property(n => n.Title).HasMaxLength(200);
        b.HasOne(n => n.Patient).WithMany(p => p.ClinicalNotes)
            .HasForeignKey(n => n.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(n => n.Addenda).WithOne(a => a.ClinicalNote!)
            .HasForeignKey(a => a.ClinicalNoteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(n => n.Provider).WithMany()
            .HasForeignKey(n => n.ProviderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(n => n.Appointment).WithMany()
            .HasForeignKey(n => n.AppointmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PatientDiagnosisConfiguration : IEntityTypeConfiguration<PatientDiagnosis>
{
    public void Configure(EntityTypeBuilder<PatientDiagnosis> b)
    {
        b.ToTable("PatientDiagnoses");
        b.Property(d => d.Code).HasMaxLength(20).IsRequired();
        b.Property(d => d.CodeSystem).HasMaxLength(30);
        b.Property(d => d.Description).HasMaxLength(300).IsRequired();
        b.HasIndex(d => new { d.PatientId, d.Status });
        b.HasOne(d => d.Patient).WithMany(p => p.Diagnoses)
            .HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(d => d.Tooth).WithMany()
            .HasForeignKey(d => d.ToothId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(d => d.DiagnosedByStaff).WithMany()
            .HasForeignKey(d => d.DiagnosedByStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class MedicalConditionConfiguration : IEntityTypeConfiguration<MedicalCondition>
{
    public void Configure(EntityTypeBuilder<MedicalCondition> b)
    {
        b.ToTable("MedicalConditions");
        b.Property(c => c.Code).HasMaxLength(30).IsRequired();
        b.Property(c => c.Name).HasMaxLength(160).IsRequired();
        b.Property(c => c.Category).HasMaxLength(80);
        b.HasIndex(c => c.Code).IsUnique();
    }
}

public class PatientMedicalConditionConfiguration : IEntityTypeConfiguration<PatientMedicalCondition>
{
    public void Configure(EntityTypeBuilder<PatientMedicalCondition> b)
    {
        b.ToTable("PatientMedicalConditions");
        b.Property(c => c.FreeTextCondition).HasMaxLength(200);
        b.HasIndex(c => new { c.PatientId, c.Status });
        b.HasOne(c => c.MedicalCondition).WithMany(m => m.PatientLinks)
            .HasForeignKey(c => c.MedicalConditionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AllergenConfiguration : IEntityTypeConfiguration<Allergen>
{
    public void Configure(EntityTypeBuilder<Allergen> b)
    {
        b.ToTable("Allergens");
        b.Property(a => a.Code).HasMaxLength(30).IsRequired();
        b.Property(a => a.Name).HasMaxLength(160).IsRequired();
        b.HasIndex(a => a.Code).IsUnique();
    }
}

public class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> b)
    {
        b.ToTable("PatientAllergies");
        b.Property(a => a.FreeTextAllergen).HasMaxLength(200);
        b.Property(a => a.Reaction).HasMaxLength(500);
        b.HasIndex(a => new { a.PatientId, a.IsActive });
        b.HasOne(a => a.Allergen).WithMany()
            .HasForeignKey(a => a.AllergenId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> b)
    {
        b.ToTable("Medications");
        b.Property(m => m.Code).HasMaxLength(30).IsRequired();
        b.Property(m => m.Name).HasMaxLength(200).IsRequired();
        b.Property(m => m.GenericName).HasMaxLength(200);
        b.Property(m => m.DrugClass).HasMaxLength(120);
        b.HasIndex(m => m.Code).IsUnique();
        b.HasIndex(m => m.Name);
    }
}

public class PatientMedicationConfiguration : IEntityTypeConfiguration<PatientMedication>
{
    public void Configure(EntityTypeBuilder<PatientMedication> b)
    {
        b.ToTable("PatientMedications");
        b.Property(m => m.FreeTextMedication).HasMaxLength(200);
        b.HasIndex(m => new { m.PatientId, m.IsCurrent });
        b.HasOne(m => m.Medication).WithMany()
            .HasForeignKey(m => m.MedicationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MedicalHistoryReviewConfiguration : IEntityTypeConfiguration<MedicalHistoryReview>
{
    public void Configure(EntityTypeBuilder<MedicalHistoryReview> b)
    {
        b.ToTable("MedicalHistoryReviews");
        b.HasIndex(r => new { r.PatientId, r.ReviewDate });
        b.HasOne(r => r.Patient).WithMany(p => p.MedicalHistoryReviews)
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.ReviewedByStaff).WithMany()
            .HasForeignKey(r => r.ReviewedByStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class VitalSignRecordConfiguration : IEntityTypeConfiguration<VitalSignRecord>
{
    public void Configure(EntityTypeBuilder<VitalSignRecord> b)
    {
        b.ToTable("VitalSignRecords");
        b.HasIndex(v => new { v.PatientId, v.RecordedAtUtc });
        b.HasOne(v => v.Patient).WithMany(p => p.VitalSigns)
            .HasForeignKey(v => v.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(v => v.RecordedByStaff).WithMany()
            .HasForeignKey(v => v.RecordedByStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> b)
    {
        b.ToTable("Prescriptions");
        b.Property(p => p.PrescriptionNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(p => p.PrescriptionNumber).IsUnique();
        b.HasIndex(p => new { p.PatientId, p.IssueDate });
        b.HasOne(p => p.Patient).WithMany(x => x.Prescriptions)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Items).WithOne(i => i.Prescription!)
            .HasForeignKey(i => i.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.PrescriberStaff).WithMany()
            .HasForeignKey(p => p.PrescriberStaffId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.Pharmacy).WithMany()
            .HasForeignKey(p => p.PharmacyId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> b)
    {
        b.ToTable("PrescriptionItems");
        b.Property(i => i.Dosage).HasMaxLength(120);
        b.Property(i => i.Frequency).HasMaxLength(120);
        b.Property(i => i.Instructions).HasMaxLength(1000);
        b.HasOne(i => i.Medication).WithMany()
            .HasForeignKey(i => i.MedicationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PharmacyConfiguration : IEntityTypeConfiguration<Pharmacy>
{
    public void Configure(EntityTypeBuilder<Pharmacy> b)
    {
        b.ToTable("Pharmacies");
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.OwnsOne(p => p.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(p => p.Address).IsRequired();
        b.OwnsOne(p => p.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(p => p.Contact).IsRequired();
    }
}

public class RadiographRecordConfiguration : IEntityTypeConfiguration<RadiographRecord>
{
    public void Configure(EntityTypeBuilder<RadiographRecord> b)
    {
        b.ToTable("RadiographRecords");
        b.HasIndex(r => new { r.PatientId, r.TakenAtUtc });
        b.Property(r => r.ToothNumbers).HasMaxLength(200);
        b.Property(r => r.Findings).HasMaxLength(4000);
        b.HasOne(r => r.Patient).WithMany(p => p.Radiographs)
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.Document).WithMany()
            .HasForeignKey(r => r.DocumentId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(r => r.TakenByStaff).WithMany()
            .HasForeignKey(r => r.TakenByStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConsentFormTemplateConfiguration : IEntityTypeConfiguration<ConsentFormTemplate>
{
    public void Configure(EntityTypeBuilder<ConsentFormTemplate> b)
    {
        b.ToTable("ConsentFormTemplates");
        b.Property(t => t.Code).HasMaxLength(40).IsRequired();
        b.Property(t => t.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(t => t.Code).IsUnique();
    }
}

public class PatientConsentConfiguration : IEntityTypeConfiguration<PatientConsent>
{
    public void Configure(EntityTypeBuilder<PatientConsent> b)
    {
        b.ToTable("PatientConsents");
        b.HasIndex(c => new { c.PatientId, c.Status });
        b.HasOne(c => c.Patient).WithMany(p => p.Consents)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.ConsentFormTemplate).WithMany()
            .HasForeignKey(c => c.ConsentFormTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(c => c.WitnessStaff).WithMany()
            .HasForeignKey(c => c.WitnessStaffId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(c => c.ClinicianStaff).WithMany()
            .HasForeignKey(c => c.ClinicianStaffId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> b)
    {
        b.ToTable("Referrals");
        b.Property(r => r.ReferralNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(r => r.ReferralNumber).IsUnique();
        b.Property(r => r.Reason).HasMaxLength(1000);
        b.OwnsOne(r => r.ExternalContact, c => OwnedTypeMapping.MapContact(c, "ExtContact"));
        b.Navigation(r => r.ExternalContact).IsRequired();
        b.OwnsOne(r => r.ExternalAddress, a => OwnedTypeMapping.MapOptionalAddress(a, "ExtAddress"));
        b.HasOne(r => r.Patient).WithMany(p => p.Referrals)
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.InternalProvider).WithMany()
            .HasForeignKey(r => r.InternalProviderId).OnDelete(DeleteBehavior.SetNull);
    }
}
