using DentalSurgery.Application.Scheduling;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using Xunit;

namespace DentalSurgery.Tests;

public class AvailabilityCalculatorTests
{
    private readonly AvailabilityCalculator _calculator = new();

    private static readonly Guid ProviderId = Guid.NewGuid();
    private static readonly Guid OperatoryId = Guid.NewGuid();
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();

    /// <summary>A Wednesday, so weekday rota rules apply.</summary>
    private static readonly DateOnly Date = new(2026, 9, 9);

    private static Staff Provider() => new()
    {
        Id = ProviderId,
        Name = new PersonName { FirstName = "Ada", LastName = "Okafor" },
        IsProvider = true,
        IsActive = true
    };

    private static StaffScheduleSlot Rota(int startHour = 9, int endHour = 17) => new()
    {
        StaffId = ProviderId,
        LocationId = LocationId,
        DayOfWeek = DayOfWeek.Wednesday,
        StartTime = new TimeSpan(startHour, 0, 0),
        EndTime = new TimeSpan(endHour, 0, 0),
        EffectiveFrom = new DateOnly(2020, 1, 1),
        IsActive = true
    };

    private static Appointment Booking(int startHour, int startMinute, int durationMinutes,
        Guid? providerId = null, Guid? operatoryId = null, Guid? patientId = null,
        AppointmentStatus status = AppointmentStatus.Confirmed)
    {
        var start = Date.ToDateTime(new TimeOnly(startHour, startMinute));
        return new Appointment
        {
            PatientId = patientId ?? PatientId,
            ProviderId = providerId ?? ProviderId,
            OperatoryId = operatoryId ?? OperatoryId,
            LocationId = LocationId,
            StartUtc = start,
            EndUtc = start.AddMinutes(durationMinutes),
            Status = status
        };
    }

    private static BusinessHours Hours(int open = 8, int close = 18) => new()
    {
        LocationId = LocationId,
        DayOfWeek = DayOfWeek.Wednesday,
        OpenTime = new TimeSpan(open, 0, 0),
        CloseTime = new TimeSpan(close, 0, 0)
    };

    // ---------------------------------------------------------------- validation

    [Fact]
    public void A_clear_slot_produces_no_conflicts()
    {
        var start = Date.ToDateTime(new TimeOnly(10, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), ProviderId, OperatoryId, PatientId,
            Array.Empty<Appointment>(), new[] { Rota() }, null, new[] { Hours() });

        Assert.Empty(conflicts);
    }

    [Fact]
    public void An_end_before_the_start_is_rejected()
    {
        var start = Date.ToDateTime(new TimeOnly(10, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(-30), ProviderId, OperatoryId, PatientId, Array.Empty<Appointment>());

        Assert.Single(conflicts);
        Assert.Equal("INVALID_RANGE", conflicts[0].Code);
    }

    [Fact]
    public void A_provider_cannot_be_double_booked()
    {
        var existing = Booking(10, 0, 30);
        var start = Date.ToDateTime(new TimeOnly(10, 15));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), ProviderId, Guid.NewGuid(), Guid.NewGuid(),
            new[] { existing }, new[] { Rota() }, null, new[] { Hours() });

        Assert.Contains(conflicts, c => c.Code == "PROVIDER_BUSY");
    }

    [Fact]
    public void A_surgery_cannot_be_double_booked()
    {
        var existing = Booking(10, 0, 60);
        var start = Date.ToDateTime(new TimeOnly(10, 30));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), Guid.NewGuid(), OperatoryId, Guid.NewGuid(),
            new[] { existing }, null, null, new[] { Hours() });

        Assert.Contains(conflicts, c => c.Code == "OPERATORY_BUSY");
    }

    [Fact]
    public void A_patient_cannot_be_in_two_places_at_once()
    {
        var existing = Booking(11, 0, 30);
        var start = Date.ToDateTime(new TimeOnly(11, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), Guid.NewGuid(), Guid.NewGuid(), PatientId,
            new[] { existing }, null, null, new[] { Hours() });

        Assert.Contains(conflicts, c => c.Code == "PATIENT_CLASH");
    }

    [Fact]
    public void A_cancelled_booking_frees_the_slot()
    {
        var cancelled = Booking(10, 0, 30, status: AppointmentStatus.Cancelled);
        var start = Date.ToDateTime(new TimeOnly(10, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), ProviderId, OperatoryId, Guid.NewGuid(),
            new[] { cancelled }, new[] { Rota() }, null, new[] { Hours() });

        Assert.Empty(conflicts);
    }

    [Fact]
    public void Editing_an_appointment_does_not_clash_with_itself()
    {
        var existing = Booking(10, 0, 30);
        var start = Date.ToDateTime(new TimeOnly(10, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(45), ProviderId, OperatoryId, PatientId,
            new[] { existing }, new[] { Rota() }, null, new[] { Hours() },
            ignoreAppointmentId: existing.Id);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void Booking_outside_the_opening_hours_is_flagged()
    {
        var start = Date.ToDateTime(new TimeOnly(19, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), ProviderId, OperatoryId, PatientId,
            Array.Empty<Appointment>(), null, null, new[] { Hours(8, 18) });

        Assert.Contains(conflicts, c => c.Code == "OUTSIDE_HOURS");
    }

    [Fact]
    public void A_clinic_closure_blocks_the_day()
    {
        var closure = new ClinicClosure
        {
            LocationId = LocationId,
            StartDate = Date,
            EndDate = Date,
            Reason = "Staff training",
            BlocksBooking = true
        };

        var start = Date.ToDateTime(new TimeOnly(10, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), ProviderId, OperatoryId, PatientId,
            Array.Empty<Appointment>(), new[] { Rota() }, null, new[] { Hours() }, new[] { closure });

        Assert.Contains(conflicts, c => c.Code == "CLINIC_CLOSED");
    }

    [Fact]
    public void Approved_leave_blocks_the_provider()
    {
        var leave = new StaffTimeOff
        {
            StaffId = ProviderId,
            StartUtc = Date.ToDateTime(TimeOnly.MinValue),
            EndUtc = Date.AddDays(1).ToDateTime(TimeOnly.MinValue),
            Reason = "Annual leave",
            IsApproved = true
        };

        var start = Date.ToDateTime(new TimeOnly(10, 0));

        var conflicts = _calculator.Validate(
            start, start.AddMinutes(30), ProviderId, OperatoryId, PatientId,
            Array.Empty<Appointment>(), new[] { Rota() }, new[] { leave }, new[] { Hours() });

        Assert.Contains(conflicts, c => c.Code == "PROVIDER_AWAY");
    }

    // ---------------------------------------------------------------- slot finding

    [Fact]
    public void An_empty_diary_yields_slots_across_the_whole_rota()
    {
        var slots = _calculator.FindSlots(
            Date, Provider(), 30, Array.Empty<Appointment>(), new[] { Rota(9, 12) });

        // 09:00 to 12:00 in 15-minute steps, with the last 30-minute slot starting at 11:30.
        Assert.Equal(11, slots.Count);
        Assert.Equal(new TimeSpan(9, 0, 0), slots[0].StartUtc.TimeOfDay);
        Assert.Equal(new TimeSpan(11, 30, 0), slots[^1].StartUtc.TimeOfDay);
    }

    [Fact]
    public void Existing_bookings_are_excluded_from_the_slot_list()
    {
        var existing = Booking(10, 0, 60);

        var slots = _calculator.FindSlots(
            Date, Provider(), 30, new[] { existing }, new[] { Rota(9, 12) });

        Assert.DoesNotContain(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(10, 0, 0));
        Assert.DoesNotContain(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(10, 30, 0));
        // 09:45 would overlap the 10:00 booking and must also be excluded.
        Assert.DoesNotContain(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(9, 45, 0));
        Assert.Contains(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(9, 30, 0));
        Assert.Contains(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(11, 0, 0));
    }

    [Fact]
    public void A_rota_break_removes_the_slots_it_covers()
    {
        var rota = Rota(9, 13);
        rota.BreakStart = new TimeSpan(11, 0, 0);
        rota.BreakEnd = new TimeSpan(12, 0, 0);

        var slots = _calculator.FindSlots(Date, Provider(), 30, Array.Empty<Appointment>(), new[] { rota });

        Assert.DoesNotContain(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(11, 0, 0));
        Assert.DoesNotContain(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(11, 30, 0));
        Assert.Contains(slots, s => s.StartUtc.TimeOfDay == new TimeSpan(12, 0, 0));
    }

    [Fact]
    public void A_day_the_provider_does_not_work_yields_nothing()
    {
        var thursday = new DateOnly(2026, 9, 10);

        var slots = _calculator.FindSlots(
            thursday, Provider(), 30, Array.Empty<Appointment>(), new[] { Rota() });

        Assert.Empty(slots);
    }

    [Fact]
    public void A_duration_longer_than_the_rota_yields_nothing()
    {
        var slots = _calculator.FindSlots(
            Date, Provider(), 300, Array.Empty<Appointment>(), new[] { Rota(9, 12) });

        Assert.Empty(slots);
    }

    // ---------------------------------------------------------------- utilisation

    [Fact]
    public void Utilisation_is_booked_minutes_over_rostered_minutes()
    {
        var rota = Rota(9, 13);   // four hours = 240 minutes
        var bookings = new[] { Booking(9, 0, 60), Booking(11, 0, 60) };

        var utilisation = _calculator.CalculateUtilisation(Date, ProviderId, bookings, new[] { rota });

        Assert.Equal(50m, utilisation);
    }

    [Fact]
    public void Utilisation_excludes_the_rota_break_from_the_denominator()
    {
        var rota = Rota(9, 13);
        rota.BreakStart = new TimeSpan(11, 0, 0);
        rota.BreakEnd = new TimeSpan(12, 0, 0);   // 180 available minutes

        var bookings = new[] { Booking(9, 0, 90) };

        var utilisation = _calculator.CalculateUtilisation(Date, ProviderId, bookings, new[] { rota });

        Assert.Equal(50m, utilisation);
    }

    [Fact]
    public void Utilisation_is_zero_when_the_provider_is_not_rostered()
    {
        var utilisation = _calculator.CalculateUtilisation(
            Date, ProviderId, Array.Empty<Appointment>(), Array.Empty<StaffScheduleSlot>());

        Assert.Equal(0m, utilisation);
    }
}
