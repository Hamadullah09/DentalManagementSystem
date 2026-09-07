using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalSurgery.Web.Api;

/// <summary>Management reporting endpoints.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = Policies.CanViewReports)]
[Produces("application/json")]
public class ReportsController(ReportingService reporting, IDateTimeProvider clock) : ControllerBase
{
    /// <summary>The dashboard figures for a given day.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var dashboard = await reporting.GetDashboardAsync(date, null, ct);
        return Ok(dashboard);
    }

    /// <summary>Production, collections and receivables for a period.</summary>
    [HttpGet("financial")]
    public async Task<IActionResult> Financial(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var range = ResolveRange(from, to);
        if (range.Error is not null) return BadRequest(range.Error);

        return Ok(await reporting.GetFinancialSummaryAsync(range.From, range.To, ct));
    }

    /// <summary>Appointment book utilisation, no-shows and cancellations.</summary>
    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var range = ResolveRange(from, to);
        if (range.Error is not null) return BadRequest(range.Error);

        return Ok(await reporting.GetScheduleAnalyticsAsync(range.From, range.To, ct));
    }

    /// <summary>Clinical activity and surgical outcomes.</summary>
    [HttpGet("clinical")]
    public async Task<IActionResult> Clinical(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var range = ResolveRange(from, to);
        if (range.Error is not null) return BadRequest(range.Error);

        return Ok(await reporting.GetClinicalAnalyticsAsync(range.From, range.To, ct));
    }

    /// <summary>Accounts receivable, aged by invoice due date.</summary>
    [HttpGet("receivables")]
    public async Task<IActionResult> Receivables(CancellationToken ct)
    {
        var aging = await reporting.GetAgingAsync(ct);
        var rows = await reporting.GetReceivablesAsync(ct);

        return Ok(new
        {
            Summary = new
            {
                aging.Current, aging.Days1To30, aging.Days31To60,
                aging.Days61To90, aging.Over90Days, aging.Total, aging.PercentOver90
            },
            Accounts = rows
        });
    }

    /// <summary>Patients due or overdue for recall.</summary>
    [HttpGet("recalls")]
    public async Task<IActionResult> Recalls([FromQuery] DateOnly? dueBy, CancellationToken ct) =>
        Ok(await reporting.GetRecallsDueAsync(dueBy, false, ct));

    /// <summary>Treatment plan acceptance for a period.</summary>
    [HttpGet("treatment-acceptance")]
    public async Task<IActionResult> TreatmentAcceptance(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var range = ResolveRange(from, to);
        if (range.Error is not null) return BadRequest(range.Error);

        return Ok(await reporting.GetTreatmentAcceptanceAsync(range.From, range.To, ct));
    }

    /// <summary>Defaults to the current month and rejects ranges that would scan too much history.</summary>
    private (DateOnly From, DateOnly To, ProblemDetails? Error) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var end = to ?? clock.Today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);

        if (end < start)
            return (start, end, new ProblemDetails { Title = "The end date is before the start date.", Status = 400 });

        if (end.DayNumber - start.DayNumber > 1095)
            return (start, end, new ProblemDetails { Title = "The range cannot exceed three years.", Status = 400 });

        return (start, end, null);
    }
}
