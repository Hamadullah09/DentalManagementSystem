using DentalSurgery.Domain.Common;

namespace DentalSurgery.Domain.Entities;

/// <summary>The dental business itself. A single row in most deployments.</summary>
public class Practice : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public string? RegulatorNumber { get; set; }

    public Address Address { get; set; } = new();
    public ContactDetails Contact { get; set; } = new();

    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public string CurrencyCode { get; set; } = "GBP";
    public string CurrencySymbol { get; set; } = "£";
    public string TimeZoneId { get; set; } = "GMT Standard Time";
    public string Locale { get; set; } = "en-GB";

    public int DefaultAppointmentMinutes { get; set; } = 30;
    public int DefaultRecallIntervalMonths { get; set; } = 6;
    public decimal DefaultTaxRatePercent { get; set; }
    public int InvoicePaymentTermDays { get; set; } = 30;
    public string? InvoiceFooterText { get; set; }
    public string? BankDetails { get; set; }

    public ICollection<Location> Locations { get; set; } = new List<Location>();
}

/// <summary>A physical site/clinic belonging to the practice.</summary>
public class Location : TenantEntity
{
    public Guid PracticeId { get; set; }
    public Practice? Practice { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public ContactDetails Contact { get; set; } = new();
    public string? ColourHex { get; set; } = "#0d6efd";
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }

    public ICollection<Operatory> Operatories { get; set; } = new List<Operatory>();
    public ICollection<BusinessHours> BusinessHours { get; set; } = new List<BusinessHours>();
    public ICollection<ClinicClosure> Closures { get; set; } = new List<ClinicClosure>();
}

/// <summary>A treatment room / surgery / chair that appointments are booked into.</summary>
public class Operatory : TenantEntity
{
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ColourHex { get; set; } = "#6c757d";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>True when the room is equipped and licensed for surgical procedures.</summary>
    public bool IsSurgicalSuite { get; set; }
    public bool HasXRay { get; set; }
    public bool HasIntraoralScanner { get; set; }
    public bool HasSedationEquipment { get; set; }
    public bool IsWheelchairAccessible { get; set; } = true;
    public string? EquipmentNotes { get; set; }
}

/// <summary>Regular opening hours for a location, one row per weekday.</summary>
public class BusinessHours : TenantEntity
{
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeSpan OpenTime { get; set; } = new(9, 0, 0);
    public TimeSpan CloseTime { get; set; } = new(17, 0, 0);
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
}

/// <summary>A dated exception to the regular opening hours (bank holiday, training day).</summary>
public class ClinicClosure : TenantEntity
{
    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool BlocksBooking { get; set; } = true;
}
