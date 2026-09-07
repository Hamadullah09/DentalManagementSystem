using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalSurgery.Infrastructure.Persistence.Configurations;

/// <summary>Shared mapping helpers for the owned value objects.</summary>
internal static class OwnedTypeMapping
{
    /// <summary>
    /// Maps a required address. Country is stored NOT NULL with a default so the
    /// owned block always materialises, even when the rest of the address is blank.
    /// </summary>
    public static void MapAddress<T>(OwnedNavigationBuilder<T, Address> b) where T : class =>
        MapAddress(b, "Address");

    public static void MapAddress<T>(OwnedNavigationBuilder<T, Address> b, string prefix) where T : class
    {
        b.Property(a => a.Line1).HasMaxLength(200).HasColumnName($"{prefix}_Line1");
        b.Property(a => a.Line2).HasMaxLength(200).HasColumnName($"{prefix}_Line2");
        b.Property(a => a.City).HasMaxLength(100).HasColumnName($"{prefix}_City");
        b.Property(a => a.County).HasMaxLength(100).HasColumnName($"{prefix}_County");
        b.Property(a => a.PostCode).HasMaxLength(20).HasColumnName($"{prefix}_PostCode");
        b.Property(a => a.Country).HasMaxLength(100).HasColumnName($"{prefix}_Country")
            .IsRequired().HasDefaultValue("United Kingdom");
    }

    public static void MapOptionalAddress<T>(OwnedNavigationBuilder<T, Address> b, string prefix) where T : class
    {
        b.Property(a => a.Line1).HasMaxLength(200).HasColumnName($"{prefix}_Line1");
        b.Property(a => a.Line2).HasMaxLength(200).HasColumnName($"{prefix}_Line2");
        b.Property(a => a.City).HasMaxLength(100).HasColumnName($"{prefix}_City");
        b.Property(a => a.County).HasMaxLength(100).HasColumnName($"{prefix}_County");
        b.Property(a => a.PostCode).HasMaxLength(20).HasColumnName($"{prefix}_PostCode");
        b.Property(a => a.Country).HasMaxLength(100).HasColumnName($"{prefix}_Country");
    }

    /// <summary>FirstName and LastName are never null, which keeps the owned block present.</summary>
    public static void MapName<T>(OwnedNavigationBuilder<T, PersonName> b) where T : class =>
        MapName(b, "Name");

    public static void MapName<T>(OwnedNavigationBuilder<T, PersonName> b, string prefix) where T : class
    {
        b.Property(n => n.Title).HasMaxLength(20).HasColumnName($"{prefix}_Title");
        b.Property(n => n.FirstName).HasMaxLength(100).IsRequired().HasDefaultValue("").HasColumnName($"{prefix}_First");
        b.Property(n => n.MiddleName).HasMaxLength(100).HasColumnName($"{prefix}_Middle");
        b.Property(n => n.LastName).HasMaxLength(100).IsRequired().HasDefaultValue("").HasColumnName($"{prefix}_Last");
        b.Property(n => n.PreferredName).HasMaxLength(100).HasColumnName($"{prefix}_Preferred");
        b.Property(n => n.Suffix).HasMaxLength(20).HasColumnName($"{prefix}_Suffix");
    }

    public static void MapOptionalName<T>(OwnedNavigationBuilder<T, PersonName> b, string prefix) where T : class
    {
        b.Property(n => n.Title).HasMaxLength(20).HasColumnName($"{prefix}_Title");
        b.Property(n => n.FirstName).HasMaxLength(100).HasColumnName($"{prefix}_First");
        b.Property(n => n.MiddleName).HasMaxLength(100).HasColumnName($"{prefix}_Middle");
        b.Property(n => n.LastName).HasMaxLength(100).HasColumnName($"{prefix}_Last");
        b.Property(n => n.PreferredName).HasMaxLength(100).HasColumnName($"{prefix}_Preferred");
        b.Property(n => n.Suffix).HasMaxLength(20).HasColumnName($"{prefix}_Suffix");
    }

    /// <summary>PreferredContactMethod is NOT NULL so the owned block always materialises.</summary>
    public static void MapContact<T>(OwnedNavigationBuilder<T, ContactDetails> b) where T : class =>
        MapContact(b, "Contact");

    public static void MapContact<T>(OwnedNavigationBuilder<T, ContactDetails> b, string prefix) where T : class
    {
        b.Property(c => c.MobilePhone).HasMaxLength(40).HasColumnName($"{prefix}_Mobile");
        b.Property(c => c.HomePhone).HasMaxLength(40).HasColumnName($"{prefix}_Home");
        b.Property(c => c.WorkPhone).HasMaxLength(40).HasColumnName($"{prefix}_Work");
        b.Property(c => c.Email).HasMaxLength(256).HasColumnName($"{prefix}_Email");
        b.Property(c => c.PreferredContactMethod).HasMaxLength(40).HasColumnName($"{prefix}_Preferred")
            .IsRequired().HasDefaultValue("Any");
    }
}

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> b)
    {
        b.ToTable("Patients");

        b.Property(p => p.PatientNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(p => p.PatientNumber).IsUnique();
        b.HasIndex(p => p.Status);
        b.HasIndex(p => p.NextRecallDue);
        b.HasIndex(p => p.DateOfBirth);

        b.OwnsOne(p => p.Name, n =>
        {
            OwnedTypeMapping.MapName(n);
            n.HasIndex(x => x.LastName);
        });
        b.Navigation(p => p.Name).IsRequired();

        b.OwnsOne(p => p.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(p => p.Contact).IsRequired();

        b.OwnsOne(p => p.Address, c => OwnedTypeMapping.MapAddress(c));
        b.Navigation(p => p.Address).IsRequired();

        b.Property(p => p.NhsNumber).HasMaxLength(20);
        b.Property(p => p.NationalInsuranceNumber).HasMaxLength(20);
        b.Property(p => p.PreferredLanguage).HasMaxLength(60);
        b.Property(p => p.Occupation).HasMaxLength(120);
        b.Property(p => p.Employer).HasMaxLength(160);
        b.Property(p => p.ReferralSource).HasMaxLength(160);
        b.Property(p => p.Notes).HasMaxLength(4000);

        b.HasOne(p => p.PrimaryProvider).WithMany()
            .HasForeignKey(p => p.PrimaryProviderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.PrimaryHygienist).WithMany()
            .HasForeignKey(p => p.PrimaryHygienistId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.PreferredLocation).WithMany()
            .HasForeignKey(p => p.PreferredLocationId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.ReferredByPatient).WithMany()
            .HasForeignKey(p => p.ReferredByPatientId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(p => p.GuarantorPatient).WithMany()
            .HasForeignKey(p => p.GuarantorPatientId).OnDelete(DeleteBehavior.SetNull);

        // The patient aggregate owns these child collections outright.
        b.HasMany(p => p.Contacts).WithOne(c => c.Patient!)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Alerts).WithOne(a => a.Patient!)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Allergies).WithOne(a => a.Patient!)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.MedicalConditions).WithOne(c => c.Patient!)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Medications).WithOne(m => m.Patient!)
            .HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Invoices).WithOne(i => i.Patient!)
            .HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PatientContactConfiguration : IEntityTypeConfiguration<PatientContact>
{
    public void Configure(EntityTypeBuilder<PatientContact> b)
    {
        b.ToTable("PatientContacts");
        b.OwnsOne(c => c.Name, OwnedTypeMapping.MapName);
        b.Navigation(c => c.Name).IsRequired();
        b.OwnsOne(c => c.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(c => c.Contact).IsRequired();
        b.OwnsOne(c => c.Address, a => OwnedTypeMapping.MapOptionalAddress(a, "Address"));
        b.HasIndex(c => c.PatientId);
    }
}

public class PatientAlertConfiguration : IEntityTypeConfiguration<PatientAlert>
{
    public void Configure(EntityTypeBuilder<PatientAlert> b)
    {
        b.ToTable("PatientAlerts");
        b.Property(a => a.Title).HasMaxLength(200).IsRequired();
        b.Property(a => a.Detail).HasMaxLength(2000);
        b.HasIndex(a => new { a.PatientId, a.IsActive });
    }
}

public class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> b)
    {
        b.ToTable("PatientDocuments");
        b.Property(d => d.Title).HasMaxLength(200).IsRequired();
        b.Property(d => d.FileName).HasMaxLength(260).IsRequired();
        b.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
        b.Property(d => d.ContentType).HasMaxLength(160);
        b.Property(d => d.Sha256).HasMaxLength(64);
        b.Property(d => d.Tags).HasMaxLength(500);
        b.HasIndex(d => new { d.PatientId, d.DocumentType });
        b.HasOne(d => d.Patient).WithMany(p => p.Documents)
            .HasForeignKey(d => d.PatientId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SocialHistoryConfiguration : IEntityTypeConfiguration<SocialHistory>
{
    public void Configure(EntityTypeBuilder<SocialHistory> b)
    {
        b.ToTable("SocialHistories");
        b.HasIndex(s => new { s.PatientId, s.RecordedOn });
        b.HasOne(s => s.Patient).WithMany()
            .HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> b)
    {
        b.ToTable("Staff");

        b.Property(s => s.StaffNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(s => s.StaffNumber).IsUnique();
        b.HasIndex(s => new { s.IsActive, s.IsProvider });
        b.HasIndex(s => s.ApplicationUserId);

        b.OwnsOne(s => s.Name, OwnedTypeMapping.MapName);
        b.Navigation(s => s.Name).IsRequired();
        b.OwnsOne(s => s.Contact, OwnedTypeMapping.MapContact);
        b.Navigation(s => s.Contact).IsRequired();
        b.OwnsOne(s => s.Address, a => OwnedTypeMapping.MapAddress(a));
        b.Navigation(s => s.Address).IsRequired();

        b.Property(s => s.RegistrationNumber).HasMaxLength(50);
        b.Property(s => s.ApplicationUserId).HasMaxLength(450);
        b.Property(s => s.ColourHex).HasMaxLength(9);
        b.Property(s => s.Biography).HasMaxLength(4000);

        b.HasOne(s => s.DefaultLocation).WithMany()
            .HasForeignKey(s => s.DefaultLocationId).OnDelete(DeleteBehavior.SetNull);

        b.HasMany(s => s.ScheduleSlots).WithOne(x => x.Staff!)
            .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(s => s.TimeOff).WithOne(x => x.Staff!)
            .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);

        // Disambiguate: a provider owns the collection; assistants are a separate link.
        b.HasMany(s => s.Appointments).WithOne(a => a.Provider!)
            .HasForeignKey(a => a.ProviderId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(s => s.Procedures).WithOne(p => p.Provider!)
            .HasForeignKey(p => p.ProviderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StaffScheduleSlotConfiguration : IEntityTypeConfiguration<StaffScheduleSlot>
{
    public void Configure(EntityTypeBuilder<StaffScheduleSlot> b)
    {
        b.ToTable("StaffScheduleSlots");
        b.HasIndex(s => new { s.StaffId, s.DayOfWeek });
        b.HasOne(s => s.Location).WithMany()
            .HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(s => s.DefaultOperatory).WithMany()
            .HasForeignKey(s => s.DefaultOperatoryId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class StaffTimeOffConfiguration : IEntityTypeConfiguration<StaffTimeOff>
{
    public void Configure(EntityTypeBuilder<StaffTimeOff> b)
    {
        b.ToTable("StaffTimeOff");
        b.Property(t => t.Reason).HasMaxLength(200);
        b.HasIndex(t => new { t.StaffId, t.StartUtc });
    }
}
