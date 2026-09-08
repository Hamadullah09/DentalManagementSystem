using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Exporting;
using DentalSurgery.Infrastructure.Exporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalSurgery.Web.Api;

/// <summary>
/// Report and document downloads. Reports render to CSV, Excel, PDF or JSON;
/// documents are purpose-built PDFs on the practice letterhead.
/// </summary>
[ApiController]
[Route("api/exports")]
[Authorize]
[Produces("application/json")]
public class ExportsController(ExportService exports, ILogger<ExportsController> logger) : ControllerBase
{
    /// <summary>Lists the reports available for export.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ReportsExport)]
    public IActionResult Available() =>
        Ok(new
        {
            Formats = Enum.GetNames<ExportFormat>(),
            Reports = exports.AvailableReports
                .GroupBy(r => r.Category)
                .Select(group => new
                {
                    Category = group.Key,
                    Reports = group.Select(r => new { r.Key, r.Name, r.Description, r.NeedsDateRange })
                })
        });

    /// <summary>Downloads a report in the requested format.</summary>
    [HttpGet("{key}")]
    [Authorize(Policy = Permissions.ReportsExport)]
    [Produces("text/csv", "application/pdf", "application/json",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Report(
        string key,
        [FromQuery] ExportFormat format = ExportFormat.Csv,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        if (to.HasValue && from.HasValue && to < from)
            return BadRequest(new ProblemDetails { Title = "The end date is before the start date.", Status = 400 });

        if (from.HasValue && to.HasValue && to.Value.DayNumber - from.Value.DayNumber > 1095)
            return BadRequest(new ProblemDetails { Title = "The range cannot exceed three years.", Status = 400 });

        var result = await exports.ExportReportAsync(key, format, from, to, ct);

        if (result.Failed)
            return NotFound(new ProblemDetails { Title = result.ErrorMessage, Status = 404 });

        var file = result.Value!;
        logger.LogInformation("User {User} exported {Report} as {Format}.", User.Identity?.Name, key, format);

        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------- documents

    [HttpGet("documents/invoice/{id:guid}")]
    [Authorize(Policy = Permissions.BillingView)]
    public Task<IActionResult> Invoice(Guid id, CancellationToken ct) =>
        Download(() => exports.InvoicePdfAsync(id, ct));

    [HttpGet("documents/treatment-plan/{id:guid}")]
    [Authorize(Policy = Permissions.TreatmentPlansView)]
    public Task<IActionResult> TreatmentPlan(Guid id, CancellationToken ct) =>
        Download(() => exports.TreatmentPlanPdfAsync(id, ct));

    [HttpGet("documents/prescription/{id:guid}")]
    [Authorize(Policy = Permissions.PrescriptionsView)]
    public Task<IActionResult> Prescription(Guid id, CancellationToken ct) =>
        Download(() => exports.PrescriptionPdfAsync(id, ct));

    [HttpGet("documents/claim/{id:guid}")]
    [Authorize(Policy = Permissions.BillingView)]
    public Task<IActionResult> ClaimForm(Guid id, CancellationToken ct) =>
        Download(() => exports.ClaimFormPdfAsync(id, ct));

    /// <summary>The patient's clinical summary, or a referral letter when a referral is named.</summary>
    [HttpGet("documents/clinical-summary/{patientId:guid}")]
    [Authorize(Policy = Permissions.ClinicalRecordsView)]
    public Task<IActionResult> ClinicalSummary(
        Guid patientId, [FromQuery] Guid? referralId, CancellationToken ct) =>
        Download(() => exports.ClinicalSummaryPdfAsync(patientId, referralId, ct));

    private async Task<IActionResult> Download(Func<Task<Application.Common.Result<ExportedFile>>> factory)
    {
        var result = await factory();

        if (result.Failed)
            return NotFound(new ProblemDetails { Title = result.ErrorMessage, Status = 404 });

        var file = result.Value!;
        return File(file.Content, file.ContentType, file.FileName);
    }
}
