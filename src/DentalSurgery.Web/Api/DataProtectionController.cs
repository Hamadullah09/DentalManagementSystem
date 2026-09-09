using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalSurgery.Web.Api;

/// <summary>
/// The data-protection duties a practice has to be able to discharge: hand a
/// patient their record, erase it when it may no longer be held, and show that
/// the retention policy is actually running.
/// </summary>
[ApiController]
[Route("api/data-protection")]
[Authorize]
public class DataProtectionController(
    DataProtectionService dataProtection,
    StorageIntegrityService integrity,
    IFileStorage storage,
    ILogger<DataProtectionController> logger) : ControllerBase
{
    /// <summary>
    /// A patient's complete record, as JSON, for a subject access request.
    /// <para>
    /// Downloaded rather than displayed: the response is the deliverable, and
    /// the practice has a month to produce it.
    /// </para>
    /// </summary>
    [HttpGet("patients/{patientId:guid}/export")]
    [Authorize(Policy = Permissions.DataProtectionExport)]
    public async Task<IActionResult> Export(Guid patientId, CancellationToken ct)
    {
        var export = await dataProtection.ExportPatientAsync(patientId, ct);

        logger.LogInformation(
            "Subject access export produced for {PatientNumber} ({Documents} document(s) listed).",
            export.PatientNumber, export.DocumentPaths.Count);

        return File(export.Json, "application/json", export.FileName);
    }

    /// <summary>
    /// Whether this record may be erased yet, and what is holding it. Always
    /// call this before offering erasure to anyone.
    /// </summary>
    [HttpGet("patients/{patientId:guid}/erasure")]
    [Authorize(Policy = Permissions.DataProtectionReview)]
    public async Task<ActionResult<ErasureAssessment>> AssessErasure(Guid patientId, CancellationToken ct) =>
        Ok(await dataProtection.AssessErasureAsync(patientId, ct));

    /// <summary>
    /// Erases a patient's identifying details, leaving the clinical and
    /// financial record intact and unattributable.
    /// <para>
    /// Irreversible. Refused while the retention period is running unless the
    /// caller overrides deliberately, which is recorded.
    /// </para>
    /// </summary>
    [HttpPost("patients/{patientId:guid}/erasure")]
    [Authorize(Policy = Permissions.DataProtectionErase)]
    public async Task<IActionResult> Erase(
        Guid patientId, [FromBody] ErasureRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { title = "An erasure must record why it was carried out." });

        ErasureOutcome outcome;

        try
        {
            outcome = await dataProtection.ErasePatientAsync(
                patientId, request.Reason, request.OverrideRetention, ct);
        }
        catch (InvalidOperationException ex)
        {
            // The retention period, or an unsettled account. A refusal the
            // caller can act on, not a server fault.
            return Conflict(new { title = "This record cannot be erased yet.", detail = ex.Message });
        }

        // The rows are gone; the files they named are deleted afterwards so a
        // failure here cannot roll back an erasure that already committed.
        var deleted = 0;
        foreach (var path in outcome.DocumentPathsToDelete)
        {
            if (await storage.DeleteAsync(path, ct)) deleted++;
            else logger.LogWarning("Could not delete {Path} during erasure; it must be removed by hand.", path);
        }

        return Ok(new
        {
            outcome.PatientNumber,
            outcome.DocumentsRemoved,
            outcome.CommunicationsRedacted,
            filesDeleted = deleted
        });
    }

    /// <summary>
    /// Deletes audit rows past the configured retention period.
    /// <para>
    /// Exposed as an endpoint so a host's scheduler can call it, since a hosted
    /// service inside IIS only runs while the application pool happens to be up.
    /// </para>
    /// </summary>
    [HttpPost("retention/purge")]
    [Authorize(Policy = Permissions.DataProtectionReview)]
    public async Task<IActionResult> PurgeRetention(CancellationToken ct)
    {
        var years = await dataProtection.ConfiguredRetentionYearsAsync(ct);
        var removed = await dataProtection.PurgeExpiredAuditAsync(ct);

        return Ok(new { retentionYears = years, auditRowsRemoved = removed });
    }

    /// <summary>
    /// Compares the document store against the database. Run it after a restore,
    /// before trusting the restore.
    /// </summary>
    [HttpGet("storage/integrity")]
    [Authorize(Policy = Permissions.DataProtectionReview)]
    public async Task<ActionResult<StorageIntegrityReport>> StorageIntegrity(CancellationToken ct)
    {
        var report = await integrity.CheckAsync(ct);

        // A failed integrity check is reported as a conflict rather than a 200
        // with a sad payload, so a scheduled caller notices without parsing it.
        return report.IsHealthy ? Ok(report) : StatusCode(StatusCodes.Status409Conflict, report);
    }
}

/// <summary>Why an erasure is being carried out, and whether retention is being overridden.</summary>
public record ErasureRequest(string Reason, bool OverrideRetention = false);
