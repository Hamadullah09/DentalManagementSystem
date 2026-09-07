using DentalSurgery.Domain.Entities;

namespace DentalSurgery.Application.Scheduling;

/// <summary>A bookable gap in a provider's diary.</summary>
public record AvailableSlot(
    DateTime StartUtc,
    DateTime EndUtc,
    Guid ProviderId,
    string ProviderName,
    Guid? OperatoryId,
    string? OperatoryName)
{
    public int DurationMinutes => (int)(EndUtc - StartUtc).TotalMinutes;
    public DateOnly Date => DateOnly.FromDateTime(StartUtc);
    public string TimeLabel => StartUtc.ToString("HH:mm");
}

/// <summary>Why a proposed booking cannot be made.</summary>
public record BookingConflict(string Code, string Message)
{
    public static BookingConflict OutsideHours(string detail) => new("OUTSIDE_HOURS", detail);
    public static BookingConflict ProviderBusy(string detail) => new("PROVIDER_BUSY", detail);
    public static BookingConflict OperatoryBusy(string detail) => new("OPERATORY_BUSY", detail);
    public static BookingConflict ProviderAway(string detail) => new("PROVIDER_AWAY", detail);
    public static BookingConflict ClinicClosed(string detail) => new("CLINIC_CLOSED", detail);
    public static BookingConflict InvalidRange(string detail) => new("INVALID_RANGE", detail);
    public static BookingConflict PatientDoubleBooked(string detail) => new("PATIENT_CLASH", detail);
}

/// <summary>
/// Works out where an appointment can go. Deliberately free of persistence
/// concerns: callers supply the day's bookings, rotas and closures.
/// </summary>
public class AvailabilityCalculator
{
    /// <summary>Validates a proposed booking against rotas, closures and existing bookings.</summary>
    public IReadOnlyList<BookingConflict> Validate(
        DateTime startUtc,
        DateTime endUtc,
        Guid? providerId,
        Guid? operatoryId,
        Guid? patientId,
        IEnumerable<Appointment> existingAppointments,
        IEnumerable<StaffScheduleSlot>? providerRota = null,
        IEnumerable<StaffTimeOff>? providerTimeOff = null,
        IEnumerable<BusinessHours>? businessHours = null,
        IEnumerable<ClinicClosure>? closures = null,
        Guid? ignoreAppointmentId = null)
    {
        var conflicts = new List<BookingConflict>();

        if (endUtc <= startUtc)
        {
            conflicts.Add(BookingConflict.InvalidRange("The end time must be after the start time."));
            return conflicts;
        }

        var date = DateOnly.FromDateTime(startUtc);
        var startTime = startUtc.TimeOfDay;
        var endTime = endUtc.TimeOfDay;

        // Clinic closures
        if (closures is not null)
        {
            var closure = closures.FirstOrDefault(c =>
                c.BlocksBooking && date >= c.StartDate && date <= c.EndDate);
            if (closure is not null)
                conflicts.Add(BookingConflict.ClinicClosed($"The clinic is closed on this date: {closure.Reason}."));
        }

        // Opening hours
        if (businessHours is not null)
        {
            var hours = businessHours.FirstOrDefault(h => h.DayOfWeek == startUtc.DayOfWeek);
            if (hours is null || hours.IsClosed)
            {
                conflicts.Add(BookingConflict.OutsideHours($"The clinic is not open on {startUtc.DayOfWeek}."));
            }
            else if (startTime < hours.OpenTime || endTime > hours.CloseTime)
            {
                conflicts.Add(BookingConflict.OutsideHours(
                    $"Outside opening hours ({hours.OpenTime:hh\\:mm}-{hours.CloseTime:hh\\:mm})."));
            }
        }

        // Provider rota and leave
        if (providerId.HasValue)
        {
            if (providerRota is not null)
            {
                var rota = providerRota.Where(r => r.StaffId == providerId && r.CoversDate(date)).ToList();
                if (rota.Count > 0 && !rota.Any(r => startTime >= r.StartTime && endTime <= r.EndTime))
                {
                    conflicts.Add(BookingConflict.OutsideHours("The provider is not rostered at this time."));
                }
            }

            if (providerTimeOff is not null)
            {
                var away = providerTimeOff.FirstOrDefault(t =>
                    t.StaffId == providerId && t.StartUtc < endUtc && startUtc < t.EndUtc);
                if (away is not null)
                    conflicts.Add(BookingConflict.ProviderAway($"The provider is unavailable: {away.Reason}."));
            }
        }

        // Existing bookings
        var active = existingAppointments
            .Where(a => a.IsActiveBooking && a.Id != ignoreAppointmentId)
            .Where(a => a.StartUtc < endUtc && startUtc < a.EndUtc)
            .ToList();

        if (providerId.HasValue)
        {
            var clash = active.FirstOrDefault(a => a.ProviderId == providerId);
            if (clash is not null)
                conflicts.Add(BookingConflict.ProviderBusy(
                    $"The provider already has a booking at {clash.StartUtc:HH:mm}-{clash.EndUtc:HH:mm}."));
        }

        if (operatoryId.HasValue)
        {
            var clash = active.FirstOrDefault(a => a.OperatoryId == operatoryId);
            if (clash is not null)
                conflicts.Add(BookingConflict.OperatoryBusy(
                    $"The surgery is already in use at {clash.StartUtc:HH:mm}-{clash.EndUtc:HH:mm}."));
        }

        if (patientId.HasValue)
        {
            var clash = active.FirstOrDefault(a => a.PatientId == patientId);
            if (clash is not null)
                conflicts.Add(BookingConflict.PatientDoubleBooked(
                    $"The patient already has an appointment at {clash.StartUtc:HH:mm}."));
        }

        return conflicts;
    }

    /// <summary>Finds free slots of the requested length for one provider on one day.</summary>
    public IReadOnlyList<AvailableSlot> FindSlots(
        DateOnly date,
        Staff provider,
        int durationMinutes,
        IEnumerable<Appointment> existingAppointments,
        IEnumerable<StaffScheduleSlot> providerRota,
        IEnumerable<StaffTimeOff>? providerTimeOff = null,
        IEnumerable<ClinicClosure>? closures = null,
        Operatory? operatory = null,
        int stepMinutes = 15)
    {
        var slots = new List<AvailableSlot>();
        if (durationMinutes <= 0) return slots;

        if (closures?.Any(c => c.BlocksBooking && date >= c.StartDate && date <= c.EndDate) == true)
            return slots;

        var windows = providerRota
            .Where(r => r.StaffId == provider.Id && r.CoversDate(date))
            .ToList();
        if (windows.Count == 0) return slots;

        var busy = existingAppointments
            .Where(a => a.IsActiveBooking && a.ProviderId == provider.Id && a.Date == date)
            .Select(a => (a.StartUtc, a.EndUtc))
            .ToList();

        if (operatory is not null)
        {
            busy.AddRange(existingAppointments
                .Where(a => a.IsActiveBooking && a.OperatoryId == operatory.Id && a.Date == date)
                .Select(a => (a.StartUtc, a.EndUtc)));
        }

        if (providerTimeOff is not null)
        {
            busy.AddRange(providerTimeOff
                .Where(t => t.StaffId == provider.Id &&
                            DateOnly.FromDateTime(t.StartUtc) <= date &&
                            DateOnly.FromDateTime(t.EndUtc) >= date)
                .Select(t => (t.StartUtc, t.EndUtc)));
        }

        foreach (var window in windows)
        {
            var dayStart = date.ToDateTime(TimeOnly.FromTimeSpan(window.StartTime));
            var dayEnd = date.ToDateTime(TimeOnly.FromTimeSpan(window.EndTime));

            var breaks = new List<(DateTime, DateTime)>(busy);
            if (window.BreakStart.HasValue && window.BreakEnd.HasValue)
            {
                breaks.Add((
                    date.ToDateTime(TimeOnly.FromTimeSpan(window.BreakStart.Value)),
                    date.ToDateTime(TimeOnly.FromTimeSpan(window.BreakEnd.Value))));
            }

            for (var cursor = dayStart; cursor.AddMinutes(durationMinutes) <= dayEnd; cursor = cursor.AddMinutes(stepMinutes))
            {
                var slotEnd = cursor.AddMinutes(durationMinutes);
                var overlaps = breaks.Any(b => cursor < b.Item2 && b.Item1 < slotEnd);
                if (overlaps) continue;

                slots.Add(new AvailableSlot(
                    cursor, slotEnd, provider.Id, provider.DisplayName,
                    operatory?.Id ?? window.DefaultOperatoryId, operatory?.Name));
            }
        }

        return slots.OrderBy(s => s.StartUtc).ToList();
    }

    /// <summary>Percentage of rostered minutes that are booked, per provider, for a date.</summary>
    public decimal CalculateUtilisation(
        DateOnly date,
        Guid providerId,
        IEnumerable<Appointment> appointments,
        IEnumerable<StaffScheduleSlot> rota)
    {
        var available = rota
            .Where(r => r.StaffId == providerId && r.CoversDate(date))
            .Sum(r =>
            {
                var minutes = (r.EndTime - r.StartTime).TotalMinutes;
                if (r.BreakStart.HasValue && r.BreakEnd.HasValue)
                    minutes -= (r.BreakEnd.Value - r.BreakStart.Value).TotalMinutes;
                return minutes;
            });

        if (available <= 0) return 0m;

        var booked = appointments
            .Where(a => a.IsActiveBooking && a.ProviderId == providerId && a.Date == date)
            .Sum(a => a.DurationMinutes);

        return Math.Round((decimal)booked / (decimal)available * 100m, 1);
    }
}
