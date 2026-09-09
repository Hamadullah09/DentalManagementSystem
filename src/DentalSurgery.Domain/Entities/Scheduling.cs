using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>A booking in the appointment book, tied to a provider and an operatory.</summary>
public class Appointment : TenantEntity
{
    public string AppointmentNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }
    public Guid? AssistantId { get; set; }
    public Staff? Assistant { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }
    public Guid? OperatoryId { get; set; }
    public Operatory? Operatory { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public AppointmentType AppointmentType { get; set; } = AppointmentType.RoutineExam;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Unconfirmed;
    public TreatmentPriority Priority { get; set; } = TreatmentPriority.Routine;

    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? PrivateNotes { get; set; }
    public string? ColourHex { get; set; }

    public bool IsRecallVisit { get; set; }
    public bool IsNewPatient { get; set; }
    public bool IsEmergency { get; set; }
    public bool RequiresInterpreter { get; set; }
    public bool RequiresSedation { get; set; }
    public bool RequiresPreMedication { get; set; }
    public bool RequiresLabWork { get; set; }
    public bool LabWorkReceived { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }
    public string? ConfirmedVia { get; set; }
    public DateTime? ArrivedAtUtc { get; set; }
    public DateTime? SeatedAtUtc { get; set; }
    public DateTime? TreatmentStartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CheckedOutAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public Guid? RescheduledToAppointmentId { get; set; }
    public Guid? RescheduledFromAppointmentId { get; set; }

    public Guid? RecallScheduleId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public string? BookedBy { get; set; }
    public DateTime? BookedAtUtc { get; set; }

    public ICollection<AppointmentProcedure> PlannedProcedures { get; set; } = new List<AppointmentProcedure>();
    public ICollection<Procedure> CompletedProcedures { get; set; } = new List<Procedure>();
    public ICollection<AppointmentReminder> Reminders { get; set; } = new List<AppointmentReminder>();

    public int DurationMinutes => (int)(EndUtc - StartUtc).TotalMinutes;
    public DateOnly Date => DateOnly.FromDateTime(StartUtc);

    public bool IsActiveBooking =>
        Status is not (AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Rescheduled or AppointmentStatus.Broken);

    public int? WaitingMinutes =>
        ArrivedAtUtc.HasValue && SeatedAtUtc.HasValue
            ? (int)(SeatedAtUtc.Value - ArrivedAtUtc.Value).TotalMinutes
            : null;

    public int? ChairMinutes =>
        SeatedAtUtc.HasValue && CompletedAtUtc.HasValue
            ? (int)(CompletedAtUtc.Value - SeatedAtUtc.Value).TotalMinutes
            : null;

    /// <summary>True when this booking overlaps another in time.</summary>
    public bool OverlapsWith(DateTime otherStart, DateTime otherEnd) =>
        StartUtc < otherEnd && otherStart < EndUtc;
}

/// <summary>A procedure intended for an appointment, used for time and fee estimates.</summary>
public class AppointmentProcedure : TenantEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid ProcedureCodeId { get; set; }
    public ProcedureCode? ProcedureCode { get; set; }

    public Guid? TreatmentPlanItemId { get; set; }
    public TreatmentPlanItem? TreatmentPlanItem { get; set; }

    public Guid? ToothId { get; set; }
    public Tooth? Tooth { get; set; }
    public ToothSurface Surfaces { get; set; } = ToothSurface.None;

    public int EstimatedMinutes { get; set; } = 30;
    public decimal EstimatedFee { get; set; }
    public int Sequence { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? CompletedProcedureId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>A scheduled reminder message for an appointment.</summary>
public class AppointmentReminder : TenantEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Sms;
    public DateTime ScheduledForUtc { get; set; }
    public int HoursBeforeAppointment { get; set; } = 24;
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

    public DateTime? SentAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public string? Recipient { get; set; }
    public string? MessageBody { get; set; }
    public string? Response { get; set; }
    public string? FailureReason { get; set; }
    public int AttemptCount { get; set; }
}

/// <summary>A recurring clinical review the patient is due for.</summary>
public class RecallSchedule : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public RecallType RecallType { get; set; } = RecallType.RoutineExam;
    public string? CustomName { get; set; }
    public int IntervalMonths { get; set; } = 6;

    public DateOnly? LastCompletedDate { get; set; }
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public RecallStatus Status { get; set; } = RecallStatus.Scheduled;

    public Guid? PreferredProviderId { get; set; }
    public Staff? PreferredProvider { get; set; }
    public Guid? BookedAppointmentId { get; set; }

    public int ContactAttempts { get; set; }
    public DateOnly? LastContactedOn { get; set; }
    public DateOnly? SuspendedUntil { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public int DaysOverdue
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return DueDate >= today ? 0 : today.DayNumber - DueDate.DayNumber;
        }
    }

    public string DisplayName => RecallType == RecallType.Custom
        ? CustomName ?? "Custom recall"
        : System.Text.RegularExpressions.Regex.Replace(RecallType.ToString(), "(?<!^)([A-Z])", " $1");
}

/// <summary>A patient wanting an earlier slot if one becomes free.</summary>
public class WaitlistEntry : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid? PreferredProviderId { get; set; }
    public Staff? PreferredProvider { get; set; }
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public AppointmentType AppointmentType { get; set; } = AppointmentType.RoutineExam;
    public string? ProcedureDescription { get; set; }
    public int EstimatedMinutes { get; set; } = 30;

    public DateOnly AvailableFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? AvailableUntil { get; set; }
    public bool MondayOk { get; set; } = true;
    public bool TuesdayOk { get; set; } = true;
    public bool WednesdayOk { get; set; } = true;
    public bool ThursdayOk { get; set; } = true;
    public bool FridayOk { get; set; } = true;
    public bool SaturdayOk { get; set; }
    public bool SundayOk { get; set; }
    public bool MorningOk { get; set; } = true;
    public bool AfternoonOk { get; set; } = true;
    public bool EveningOk { get; set; }

    public WaitlistPriority Priority { get; set; } = WaitlistPriority.Normal;
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;
    public int ContactAttempts { get; set; }
    public DateTime? LastContactedUtc { get; set; }
    public Guid? BookedAppointmentId { get; set; }
    public string? Notes { get; set; }

    public bool AcceptsDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => MondayOk,
        DayOfWeek.Tuesday => TuesdayOk,
        DayOfWeek.Wednesday => WednesdayOk,
        DayOfWeek.Thursday => ThursdayOk,
        DayOfWeek.Friday => FridayOk,
        DayOfWeek.Saturday => SaturdayOk,
        DayOfWeek.Sunday => SundayOk,
        _ => false
    };
}
