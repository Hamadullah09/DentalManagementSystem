using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Common;
using DentalSurgery.Application.Scheduling;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>One day of the appointment book, arranged by operatory column.</summary>
public class DaySheet
{
    public DateOnly Date { get; init; }
    public Guid LocationId { get; init; }
    public IReadOnlyList<Operatory> Operatories { get; init; } = Array.Empty<Operatory>();
    public IReadOnlyList<Appointment> Appointments { get; init; } = Array.Empty<Appointment>();
    public IReadOnlyList<Staff> Providers { get; init; } = Array.Empty<Staff>();
    public TimeSpan DayStart { get; init; } = new(8, 0, 0);
    public TimeSpan DayEnd { get; init; } = new(18, 0, 0);
    public bool IsClosed { get; init; }
    public string? ClosureReason { get; init; }

    public IEnumerable<Appointment> ForOperatory(Guid operatoryId) =>
        Appointments.Where(a => a.OperatoryId == operatoryId).OrderBy(a => a.StartUtc);

    public IEnumerable<Appointment> Unassigned =>
        Appointments.Where(a => a.OperatoryId is null).OrderBy(a => a.StartUtc);

    public int TotalBookedMinutes => Appointments.Where(a => a.IsActiveBooking).Sum(a => a.DurationMinutes);
    public int ArrivedCount => Appointments.Count(a => a.Status == AppointmentStatus.ArrivedWaiting);
    public int InTreatmentCount => Appointments.Count(a => a.Status is AppointmentStatus.Seated or AppointmentStatus.InTreatment);
    public int CompletedCount => Appointments.Count(a => a.Status is AppointmentStatus.Completed or AppointmentStatus.CheckedOut);
    public int UnconfirmedCount => Appointments.Count(a => a.Status == AppointmentStatus.Unconfirmed);
}

/// <summary>Booking, rescheduling and the check-in workflow.</summary>
public class AppointmentService(
    DentalDbContext db,
    INumberSequenceService sequences,
    IDateTimeProvider clock,
    ILogger<AppointmentService> logger,
    IPermissionGuard guard)
{
    private readonly AvailabilityCalculator _availability = new();

    // ------------------------------------------------------------------ query

    public async Task<DaySheet> GetDaySheetAsync(
        DateOnly date, Guid? locationId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsView, ct);
        var location = locationId.HasValue
            ? await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId, ct)
            : await db.Locations.AsNoTracking().OrderByDescending(l => l.IsPrimary).FirstOrDefaultAsync(ct);

        if (location is null)
            return new DaySheet { Date = date, IsClosed = true, ClosureReason = "No clinic location is configured." };

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var appointments = await db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Include(a => a.Operatory)
            .Include(a => a.PlannedProcedures).ThenInclude(p => p.ProcedureCode)
            .Where(a => a.LocationId == location.Id && a.StartUtc >= dayStart && a.StartUtc < dayEnd)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(ct);

        var operatories = await db.Operatories.AsNoTracking()
            .Where(o => o.LocationId == location.Id && o.IsActive)
            .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
            .ToListAsync(ct);

        var providers = await db.Staff.AsNoTracking()
            .Where(s => s.IsActive && s.IsProvider)
            .OrderBy(s => s.Name.LastName)
            .ToListAsync(ct);

        var hours = await db.BusinessHours.AsNoTracking()
            .FirstOrDefaultAsync(h => h.LocationId == location.Id && h.DayOfWeek == date.DayOfWeek, ct);

        var closure = await db.ClinicClosures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.LocationId == location.Id &&
                                      date >= c.StartDate && date <= c.EndDate, ct);

        return new DaySheet
        {
            Date = date,
            LocationId = location.Id,
            Operatories = operatories,
            Appointments = appointments,
            Providers = providers,
            DayStart = hours?.OpenTime ?? new TimeSpan(8, 0, 0),
            DayEnd = hours?.CloseTime ?? new TimeSpan(18, 0, 0),
            IsClosed = closure is not null || (hours?.IsClosed ?? false),
            ClosureReason = closure?.Reason ?? (hours?.IsClosed == true ? "Closed on this weekday" : null)
        };
    }

    public async Task<List<Appointment>> GetRangeAsync(
        DateOnly from, DateOnly to, Guid? locationId = null, Guid? providerId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsView, ct);
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var query = db.Appointments.AsNoTracking()
            .Include(a => a.Patient).Include(a => a.Provider).Include(a => a.Operatory)
            .Where(a => a.StartUtc >= start && a.StartUtc < end);

        if (locationId.HasValue) query = query.Where(a => a.LocationId == locationId);
        if (providerId.HasValue) query = query.Where(a => a.ProviderId == providerId);

        return await query.OrderBy(a => a.StartUtc).ToListAsync(ct);
    }

    public async Task<Appointment?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsView, ct);
        return await db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p!.Alerts)
            .Include(a => a.Provider).Include(a => a.Assistant)
            .Include(a => a.Operatory).Include(a => a.Location)
            .Include(a => a.PlannedProcedures).ThenInclude(p => p.ProcedureCode)
            .Include(a => a.PlannedProcedures).ThenInclude(p => p.Tooth)
            .Include(a => a.CompletedProcedures).ThenInclude(p => p.ProcedureCode)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<Appointment>> GetForPatientAsync(
        Guid patientId, bool futureOnly = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsView, ct);
        var query = db.Appointments.AsNoTracking()
            .Include(a => a.Provider).Include(a => a.Operatory).Include(a => a.Location)
            .Where(a => a.PatientId == patientId);

        if (futureOnly) query = query.Where(a => a.StartUtc >= clock.UtcNow);

        return await query.OrderByDescending(a => a.StartUtc).ToListAsync(ct);
    }

    /// <summary>Free slots for a provider across a date range.</summary>
    public async Task<IReadOnlyList<AvailableSlot>> FindSlotsAsync(
        Guid providerId, DateOnly from, DateOnly to, int durationMinutes,
        Guid? operatoryId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsView, ct);
        var provider = await db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.Id == providerId, ct);
        if (provider is null) return Array.Empty<AvailableSlot>();

        var rota = await db.StaffScheduleSlots.AsNoTracking()
            .Where(r => r.StaffId == providerId && r.IsActive).ToListAsync(ct);

        var timeOff = await db.StaffTimeOff.AsNoTracking()
            .Where(t => t.StaffId == providerId && t.IsApproved).ToListAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var appointments = await db.Appointments.AsNoTracking()
            .Where(a => a.StartUtc >= start && a.StartUtc < end &&
                        (a.ProviderId == providerId || (operatoryId != null && a.OperatoryId == operatoryId)))
            .ToListAsync(ct);

        var closures = await db.ClinicClosures.AsNoTracking().ToListAsync(ct);

        var operatory = operatoryId.HasValue
            ? await db.Operatories.AsNoTracking().FirstOrDefaultAsync(o => o.Id == operatoryId, ct)
            : null;

        var slots = new List<AvailableSlot>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            slots.AddRange(_availability.FindSlots(
                date, provider, durationMinutes, appointments, rota, timeOff, closures, operatory));
        }

        return slots;
    }

    // ------------------------------------------------------------------ booking

    public async Task<Result<Appointment>> BookAsync(
        Appointment appointment, bool overrideConflicts = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsCreate, ct);
        var conflicts = await CheckConflictsAsync(appointment, ct);
        var blocking = conflicts.Where(c => c.Code != "OUTSIDE_HOURS").ToList();

        if (!overrideConflicts && conflicts.Count > 0)
            return Result<Appointment>.Failure(conflicts.Select(c => c.Message));

        if (overrideConflicts && blocking.Any(c => c.Code == "PATIENT_CLASH"))
            return Result<Appointment>.Failure(blocking.First(c => c.Code == "PATIENT_CLASH").Message);

        if (string.IsNullOrWhiteSpace(appointment.AppointmentNumber))
            appointment.AppointmentNumber = await sequences.NextAsync(SequenceNames.Appointment, ct);

        appointment.BookedAtUtc = clock.UtcNow;

        // Default reminders: one a week ahead for long-lead bookings, one the day before.
        var leadDays = (appointment.StartUtc - clock.UtcNow).TotalDays;
        if (leadDays >= 7)
        {
            appointment.Reminders.Add(new AppointmentReminder
            {
                AppointmentId = appointment.Id,
                Channel = CommunicationChannel.Email,
                HoursBeforeAppointment = 168,
                ScheduledForUtc = appointment.StartUtc.AddDays(-7),
                Status = ReminderStatus.Pending
            });
        }

        if (leadDays >= 1)
        {
            appointment.Reminders.Add(new AppointmentReminder
            {
                AppointmentId = appointment.Id,
                Channel = CommunicationChannel.Sms,
                HoursBeforeAppointment = 24,
                ScheduledForUtc = appointment.StartUtc.AddHours(-24),
                Status = ReminderStatus.Pending
            });
        }

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Booked appointment {Number} for patient {PatientId} at {Start}.",
            appointment.AppointmentNumber, appointment.PatientId, appointment.StartUtc);

        return Result<Appointment>.Success(appointment);
    }

    public async Task<IReadOnlyList<BookingConflict>> CheckConflictsAsync(
        Appointment appointment, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsView, ct);
        var dayStart = appointment.StartUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var sameDay = await db.Appointments.AsNoTracking()
            .Where(a => a.StartUtc >= dayStart && a.StartUtc < dayEnd)
            .ToListAsync(ct);

        var rota = appointment.ProviderId.HasValue
            ? await db.StaffScheduleSlots.AsNoTracking().Where(r => r.StaffId == appointment.ProviderId).ToListAsync(ct)
            : new List<StaffScheduleSlot>();

        var timeOff = appointment.ProviderId.HasValue
            ? await db.StaffTimeOff.AsNoTracking().Where(t => t.StaffId == appointment.ProviderId && t.IsApproved).ToListAsync(ct)
            : new List<StaffTimeOff>();

        var hours = await db.BusinessHours.AsNoTracking()
            .Where(h => h.LocationId == appointment.LocationId).ToListAsync(ct);

        var closures = await db.ClinicClosures.AsNoTracking()
            .Where(c => c.LocationId == appointment.LocationId).ToListAsync(ct);

        return _availability.Validate(
            appointment.StartUtc, appointment.EndUtc,
            appointment.ProviderId, appointment.OperatoryId, appointment.PatientId,
            sameDay, rota, timeOff, hours, closures, appointment.Id);
    }

    public async Task<Result> RescheduleAsync(
        Guid appointmentId, DateTime newStartUtc, int? newDurationMinutes = null,
        Guid? newOperatoryId = null, Guid? newProviderId = null,
        bool overrideConflicts = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsEdit, ct);
        var appointment = await db.Appointments
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment is null) return Result.Failure("Appointment not found.");
        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.CheckedOut)
            return Result.Failure("A completed appointment cannot be rescheduled.");

        var duration = newDurationMinutes ?? appointment.DurationMinutes;
        var originalStart = appointment.StartUtc;

        appointment.StartUtc = newStartUtc;
        appointment.EndUtc = newStartUtc.AddMinutes(duration);
        if (newOperatoryId.HasValue) appointment.OperatoryId = newOperatoryId;
        if (newProviderId.HasValue) appointment.ProviderId = newProviderId;

        var conflicts = await CheckConflictsAsync(appointment, ct);
        if (!overrideConflicts && conflicts.Count > 0)
        {
            // Put the appointment back where it was before reporting the problem.
            appointment.StartUtc = originalStart;
            appointment.EndUtc = originalStart.AddMinutes(duration);
            db.Entry(appointment).State = EntityState.Unchanged;
            return Result.Failure(conflicts.Select(c => c.Message));
        }

        // Confirmation no longer applies to the new time.
        if (appointment.Status == AppointmentStatus.Confirmed)
        {
            appointment.Status = AppointmentStatus.Unconfirmed;
            appointment.ConfirmedAtUtc = null;
        }

        foreach (var reminder in appointment.Reminders.Where(r => r.Status == ReminderStatus.Pending))
            reminder.ScheduledForUtc = appointment.StartUtc.AddHours(-reminder.HoursBeforeAppointment);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Rescheduled {Number} from {Old} to {New}.",
            appointment.AppointmentNumber, originalStart, newStartUtc);

        return Result.Success();
    }

    public async Task<Result> CancelAsync(
        Guid appointmentId, string reason, bool isNoShow = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsCancel, ct);
        var appointment = await db.Appointments
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment is null) return Result.Failure("Appointment not found.");

        appointment.Status = isNoShow ? AppointmentStatus.NoShow : AppointmentStatus.Cancelled;
        appointment.CancelledAtUtc = clock.UtcNow;
        appointment.CancellationReason = reason;

        foreach (var reminder in appointment.Reminders.Where(r => r.Status == ReminderStatus.Pending))
            reminder.Status = ReminderStatus.CancelRequested;

        // Free the slot for anyone waiting.
        var waiting = await db.WaitlistEntries
            .Where(w => w.Status == WaitlistStatus.Waiting &&
                        w.LocationId == appointment.LocationId)
            .OrderByDescending(w => w.Priority)
            .Take(5)
            .ToListAsync(ct);

        if (waiting.Count > 0)
        {
            logger.LogInformation(
                "Slot on {Date} freed; {Count} waitlist entries could fill it.",
                appointment.Date, waiting.Count(w => w.AcceptsDay(appointment.StartUtc.DayOfWeek)));
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Moves an appointment through the front-desk workflow.</summary>
    public async Task<Result> SetStatusAsync(
        Guid appointmentId, AppointmentStatus status, CancellationToken ct = default)
    {
        // Moving a patient through the visit is a waiting-room action; marking
        // one cancelled or absent is a scheduling decision with a financial and
        // recall consequence, so it is held to the stricter permission.
        await guard.DemandAsync(
            status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Broken
                ? Permissions.AppointmentsCancel
                : Permissions.WaitingRoomManage,
            ct);

        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
        if (appointment is null) return Result.Failure("Appointment not found.");

        var now = clock.UtcNow;
        switch (status)
        {
            case AppointmentStatus.Confirmed:
                appointment.ConfirmedAtUtc = now;
                appointment.ConfirmedVia = "Manual";
                break;
            case AppointmentStatus.ArrivedWaiting:
                appointment.ArrivedAtUtc ??= now;
                break;
            case AppointmentStatus.Seated:
                appointment.ArrivedAtUtc ??= now;
                appointment.SeatedAtUtc ??= now;
                break;
            case AppointmentStatus.InTreatment:
                appointment.SeatedAtUtc ??= now;
                appointment.TreatmentStartedAtUtc ??= now;
                break;
            case AppointmentStatus.Completed:
                appointment.CompletedAtUtc ??= now;
                await UpdateRecallOnCompletionAsync(appointment, ct);
                break;
            case AppointmentStatus.CheckedOut:
                appointment.CompletedAtUtc ??= now;
                appointment.CheckedOutAtUtc = now;
                break;
            case AppointmentStatus.NoShow:
                appointment.CancelledAtUtc = now;
                appointment.CancellationReason = "Did not attend";
                break;
        }

        appointment.Status = status;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Rolls the patient's recall forward once an exam or hygiene visit completes.</summary>
    private async Task UpdateRecallOnCompletionAsync(Appointment appointment, CancellationToken ct)
    {
        if (appointment.AppointmentType is not (AppointmentType.RoutineExam or AppointmentType.NewPatientExam
            or AppointmentType.Hygiene or AppointmentType.PeriodontalMaintenance))
            return;

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == appointment.PatientId, ct);
        if (patient is null) return;

        var today = DateOnly.FromDateTime(appointment.StartUtc);
        var recallType = appointment.AppointmentType switch
        {
            AppointmentType.Hygiene => RecallType.ScaleAndPolish,
            AppointmentType.PeriodontalMaintenance => RecallType.PeriodontalMaintenance,
            _ => RecallType.RoutineExam
        };

        if (recallType == RecallType.RoutineExam) patient.LastExamDate = today;
        else patient.LastHygieneDate = today;

        var recall = await db.RecallSchedules
            .FirstOrDefaultAsync(r => r.PatientId == patient.Id && r.RecallType == recallType && r.IsActive, ct);

        if (recall is null)
        {
            recall = new RecallSchedule
            {
                PatientId = patient.Id,
                RecallType = recallType,
                IntervalMonths = patient.RecallIntervalMonths,
                PreferredProviderId = appointment.ProviderId
            };
            db.RecallSchedules.Add(recall);
        }

        recall.LastCompletedDate = today;
        recall.DueDate = today.AddMonths(recall.IntervalMonths);
        recall.Status = RecallStatus.Scheduled;
        recall.ContactAttempts = 0;
        recall.BookedAppointmentId = null;

        if (recallType == RecallType.RoutineExam) patient.NextRecallDue = recall.DueDate;
    }

    public async Task<Result<Appointment>> AddPlannedProcedureAsync(
        Guid appointmentId, Guid procedureCodeId, Guid? toothId = null,
        ToothSurface surfaces = ToothSurface.None, Guid? treatmentPlanItemId = null,
        CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.AppointmentsEdit, ct);
        var appointment = await db.Appointments
            .Include(a => a.PlannedProcedures)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment is null) return Result<Appointment>.Failure("Appointment not found.");

        var code = await db.ProcedureCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == procedureCodeId, ct);
        if (code is null) return Result<Appointment>.Failure("Procedure code not found.");

        appointment.PlannedProcedures.Add(new AppointmentProcedure
        {
            AppointmentId = appointmentId,
            ProcedureCodeId = procedureCodeId,
            ToothId = toothId,
            Surfaces = surfaces,
            TreatmentPlanItemId = treatmentPlanItemId,
            EstimatedMinutes = code.DefaultDurationMinutes,
            EstimatedFee = code.DefaultFee,
            Sequence = appointment.PlannedProcedures.Count + 1
        });

        if (code.RequiresLabWork) appointment.RequiresLabWork = true;

        await db.SaveChangesAsync(ct);
        return Result<Appointment>.Success(appointment);
    }

    /// <summary>Today's arrivals board, ordered so the longest wait is at the top.</summary>
    public async Task<List<Appointment>> GetWaitingRoomAsync(Guid? locationId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.WaitingRoomView, ct);
        var today = clock.Today.ToDateTime(TimeOnly.MinValue);
        var tomorrow = today.AddDays(1);

        var query = db.Appointments.AsNoTracking()
            .Include(a => a.Patient).Include(a => a.Provider).Include(a => a.Operatory)
            .Where(a => a.StartUtc >= today && a.StartUtc < tomorrow &&
                        (a.Status == AppointmentStatus.ArrivedWaiting ||
                         a.Status == AppointmentStatus.Seated ||
                         a.Status == AppointmentStatus.InTreatment));

        if (locationId.HasValue) query = query.Where(a => a.LocationId == locationId);

        return await query.OrderBy(a => a.ArrivedAtUtc).ToListAsync(ct);
    }
}
