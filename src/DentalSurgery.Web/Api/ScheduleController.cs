using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalSurgery.Web.Api;

/// <summary>The appointment book: day sheets, availability and booking.</summary>
[ApiController]
[Route("api/schedule")]
[Authorize(Policy = Policies.CanManageSchedule)]
[Produces("application/json")]
public class ScheduleController(AppointmentService appointments, IDateTimeProvider clock) : ControllerBase
{
    /// <summary>Returns one day of the appointment book, arranged by surgery.</summary>
    [HttpGet("day")]
    public async Task<IActionResult> Day([FromQuery] DateOnly? date, [FromQuery] Guid? locationId, CancellationToken ct)
    {
        var sheet = await appointments.GetDaySheetAsync(date ?? clock.Today, locationId, ct);

        return Ok(new
        {
            sheet.Date,
            // Callers need this to book; it is not derivable from the rest of the payload.
            sheet.LocationId,
            sheet.IsClosed,
            sheet.ClosureReason,
            Operatories = sheet.Operatories.Select(o => new { o.Id, o.Code, o.Name, o.IsSurgicalSuite }),
            Providers = sheet.Providers.Select(p => new { p.Id, Name = p.DisplayName, p.ColourHex }),
            Summary = new
            {
                Booked = sheet.Appointments.Count(a => a.IsActiveBooking),
                sheet.ArrivedCount,
                sheet.InTreatmentCount,
                sheet.CompletedCount,
                sheet.UnconfirmedCount,
                sheet.TotalBookedMinutes
            },
            Appointments = sheet.Appointments.Select(a => new
            {
                a.Id,
                a.AppointmentNumber,
                a.StartUtc,
                a.EndUtc,
                a.DurationMinutes,
                Patient = a.Patient?.Name.Display,
                a.PatientId,
                Provider = a.Provider?.DisplayName,
                Operatory = a.Operatory?.Name,
                Type = a.AppointmentType.ToString(),
                Status = a.Status.ToString(),
                a.Reason
            })
        });
    }

    /// <summary>Finds free slots for a provider over a date range.</summary>
    [HttpGet("availability")]
    public async Task<IActionResult> Availability(
        [FromQuery] Guid providerId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int durationMinutes = 30,
        [FromQuery] Guid? operatoryId = null,
        CancellationToken ct = default)
    {
        if (to < from) return BadRequest(new ProblemDetails { Title = "The end date is before the start date.", Status = 400 });
        if (to.DayNumber - from.DayNumber > 60)
            return BadRequest(new ProblemDetails { Title = "The range cannot exceed 60 days.", Status = 400 });

        var slots = await appointments.FindSlotsAsync(providerId, from, to, durationMinutes, operatoryId, ct);

        return Ok(slots.Select(s => new
        {
            s.StartUtc, s.EndUtc, s.DurationMinutes, s.ProviderName, s.OperatoryName
        }));
    }

    /// <summary>Books an appointment, reporting any clash rather than creating a double booking.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book([FromBody] BookAppointmentRequest request, CancellationToken ct)
    {
        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            ProviderId = request.ProviderId,
            OperatoryId = request.OperatoryId,
            LocationId = request.LocationId,
            StartUtc = request.StartUtc,
            EndUtc = request.StartUtc.AddMinutes(request.DurationMinutes),
            AppointmentType = request.AppointmentType ?? AppointmentType.RoutineExam,
            Reason = request.Reason,
            Status = AppointmentStatus.Unconfirmed
        };

        var result = await appointments.BookAsync(appointment, request.OverrideConflicts, ct);

        if (result.Failed)
            return Conflict(new ProblemDetails
            {
                Title = "The appointment conflicts with the current book.",
                Detail = string.Join(" ", result.Errors),
                Status = 409
            });

        return CreatedAtAction(nameof(Day), new { date = DateOnly.FromDateTime(appointment.StartUtc) },
            new { result.Value!.Id, result.Value.AppointmentNumber, result.Value.StartUtc, result.Value.EndUtc });
    }

    /// <summary>Moves an appointment through the front-desk workflow.</summary>
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        var result = await appointments.SetStatusAsync(id, request.Status, ct);
        return result.Succeeded
            ? NoContent()
            : NotFound(new ProblemDetails { Title = result.ErrorMessage, Status = 404 });
    }

    /// <summary>Cancels an appointment, or records it as a non-attendance.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelRequest request, CancellationToken ct)
    {
        var result = await appointments.CancelAsync(id, request.Reason, request.NoShow, ct);
        return result.Succeeded
            ? NoContent()
            : NotFound(new ProblemDetails { Title = result.ErrorMessage, Status = 404 });
    }
}

public class BookAppointmentRequest
{
    public Guid PatientId { get; set; }
    public Guid LocationId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? OperatoryId { get; set; }
    public DateTime StartUtc { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentType? AppointmentType { get; set; }
    public string? Reason { get; set; }
    public bool OverrideConflicts { get; set; }
}

public class SetStatusRequest
{
    public AppointmentStatus Status { get; set; }
}

public class CancelRequest
{
    public string Reason { get; set; } = "Cancelled";
    public bool NoShow { get; set; }
}
