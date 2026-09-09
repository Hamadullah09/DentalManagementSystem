using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>A member of the practice team. Providers can be booked and can bill.</summary>
public class Staff : TenantEntity
{
    public string StaffNumber { get; set; } = string.Empty;

    public PersonName Name { get; set; } = new();
    public ContactDetails Contact { get; set; } = new();
    public Address Address { get; set; } = new();

    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;

    public StaffRole Role { get; set; } = StaffRole.Dentist;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public string? JobTitle { get; set; }
    public string? Specialty { get; set; }
    public string? Qualifications { get; set; }

    /// <summary>Regulator registration (GDC in the UK, licence number elsewhere).</summary>
    public string? RegistrationNumber { get; set; }
    public DateOnly? RegistrationExpiry { get; set; }
    public string? IndemnityProvider { get; set; }
    public string? IndemnityPolicyNumber { get; set; }
    public DateOnly? IndemnityExpiry { get; set; }
    public string? DbsCheckReference { get; set; }
    public DateOnly? DbsCheckDate { get; set; }
    public DateOnly? HepatitisBImmunityDate { get; set; }

    public DateOnly HireDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? TerminationDate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Providers appear on the appointment book and can be assigned procedures.</summary>
    public bool IsProvider { get; set; }
    public bool CanPrescribe { get; set; }
    public bool CanPerformSurgery { get; set; }
    public bool CanAdministerSedation { get; set; }
    public bool CanTakeRadiographs { get; set; }

    public string? ColourHex { get; set; } = "#198754";
    public Guid? DefaultLocationId { get; set; }
    public Location? DefaultLocation { get; set; }
    public int DefaultAppointmentMinutes { get; set; } = 30;

    public decimal? HourlyRate { get; set; }
    public decimal? CommissionPercent { get; set; }
    public decimal? DailyProductionTarget { get; set; }

    /// <summary>Links this staff record to an ASP.NET Identity login, when one exists.</summary>
    public string? ApplicationUserId { get; set; }

    public string? Signature { get; set; }
    public string? Biography { get; set; }
    public string? PhotoPath { get; set; }
    public string? Notes { get; set; }

    public ICollection<StaffScheduleSlot> ScheduleSlots { get; set; } = new List<StaffScheduleSlot>();
    public ICollection<StaffTimeOff> TimeOff { get; set; } = new List<StaffTimeOff>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Procedure> Procedures { get; set; } = new List<Procedure>();

    public string DisplayName
    {
        get
        {
            var title = string.IsNullOrWhiteSpace(Name.Title) ? DefaultTitle() : Name.Title;
            return string.IsNullOrWhiteSpace(title) ? Name.Display : $"{title} {Name.Display}";
        }
    }

    private string? DefaultTitle() => Role switch
    {
        StaffRole.Dentist or StaffRole.OralSurgeon or StaffRole.Orthodontist or
        StaffRole.Endodontist or StaffRole.Periodontist or StaffRole.Prosthodontist or
        StaffRole.PaediatricDentist or StaffRole.Anaesthetist => "Dr",
        _ => null
    };

    public bool RegistrationExpiringSoon =>
        RegistrationExpiry.HasValue &&
        RegistrationExpiry.Value <= DateOnly.FromDateTime(DateTime.Today.AddDays(60));
}

/// <summary>A recurring working window for a provider at a location.</summary>
public class StaffScheduleSlot : TenantEntity
{
    public Guid StaffId { get; set; }
    public Staff? Staff { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public Guid? DefaultOperatoryId { get; set; }
    public Operatory? DefaultOperatory { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }

    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public bool CoversDate(DateOnly date) =>
        IsActive &&
        date.DayOfWeek == DayOfWeek &&
        date >= EffectiveFrom &&
        (EffectiveTo is null || date <= EffectiveTo);
}

/// <summary>Leave, training or any other absence that blocks the diary.</summary>
public class StaffTimeOff : TenantEntity
{
    public Guid StaffId { get; set; }
    public Staff? Staff { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsAllDay { get; set; } = true;
    public string Reason { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Notes { get; set; }
}
