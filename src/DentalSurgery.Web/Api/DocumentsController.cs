using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalSurgery.Web.Api;

/// <summary>Upload, download and removal of patient documents and images.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class DocumentsController(DocumentService documents, ILogger<DocumentsController> logger) : ControllerBase
{
    /// <summary>Lists the documents held for a patient.</summary>
    [HttpGet("patients/{patientId:guid}/documents")]
    [Authorize(Policy = Policies.CanViewClinical)]
    public async Task<IActionResult> List(Guid patientId, CancellationToken ct)
    {
        var files = await documents.ListAsync(patientId, ct);

        return Ok(files.Select(d => new
        {
            d.Id,
            d.Title,
            d.FileName,
            Type = d.DocumentType.ToString(),
            d.ContentType,
            d.SizeBytes,
            d.UploadedAtUtc,
            d.UploadedBy,
            d.IsClinicallySignificant,
            Viewable = DocumentService.IsViewableInline(d.ContentType),
            Url = Url.Action(nameof(Download), new { documentId = d.Id })
        }));
    }

    /// <summary>Attaches a file to the patient record.</summary>
    [HttpPost("patients/{patientId:guid}/documents")]
    [Authorize(Policy = Policies.CanEditClinical)]
    [RequestSizeLimit(DocumentService.MaxUploadBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid patientId,
        IFormFile file,
        [FromForm] DocumentType documentType = DocumentType.Other,
        [FromForm] string? title = null,
        [FromForm] string? description = null,
        [FromForm] bool clinicallySignificant = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No file was supplied.", Status = 400 });

        await using var stream = file.OpenReadStream();

        var result = await documents.UploadAsync(new UploadRequest(
            patientId, stream, file.FileName, file.ContentType, file.Length,
            documentType, title, description, ClinicallySignificant: clinicallySignificant), ct);

        if (result.Failed)
            return BadRequest(new ProblemDetails { Title = result.ErrorMessage, Status = 400 });

        var document = result.Value!;
        logger.LogInformation("User {User} uploaded document {Id} for patient {PatientId}.",
            User.Identity?.Name, document.Id, patientId);

        return CreatedAtAction(nameof(Download), new { documentId = document.Id },
            new { document.Id, document.Title, document.FileName, document.ContentType, document.SizeBytes });
    }

    /// <summary>Attaches an image and records the radiographic exposure.</summary>
    [HttpPost("patients/{patientId:guid}/radiographs")]
    [Authorize(Policy = Policies.CanEditClinical)]
    [RequestSizeLimit(DocumentService.MaxUploadBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadRadiograph(
        Guid patientId,
        IFormFile file,
        [FromForm] RadiographType radiographType = RadiographType.Bitewing,
        [FromForm] string? toothNumbers = null,
        [FromForm] decimal? kiloVoltagePeak = null,
        [FromForm] decimal? exposureSeconds = null,
        [FromForm] decimal? doseMicroSieverts = null,
        [FromForm] string? justification = null,
        [FromForm] string? qualityRating = null,
        [FromForm] string? findings = null,
        [FromForm] bool isRepeat = false,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No image was supplied.", Status = 400 });

        if (string.IsNullOrWhiteSpace(justification))
            return BadRequest(new ProblemDetails
            {
                Title = "Every exposure needs a clinical justification.",
                Status = 400
            });

        await using var stream = file.OpenReadStream();

        var result = await documents.UploadRadiographAsync(
            new UploadRequest(patientId, stream, file.FileName, file.ContentType, file.Length,
                DocumentType.Radiograph, ClinicallySignificant: true),
            new RadiographUploadRequest(radiographType, toothNumbers, kiloVoltagePeak, null,
                exposureSeconds, doseMicroSieverts, justification, qualityRating, findings, isRepeat),
            ct);

        if (result.Failed)
            return BadRequest(new ProblemDetails { Title = result.ErrorMessage, Status = 400 });

        return Ok(new { result.Value!.Id, result.Value.DocumentId, Type = radiographType.ToString() });
    }

    /// <summary>Streams a stored document. Images and PDFs display inline.</summary>
    [HttpGet("documents/{documentId:guid}")]
    [Authorize(Policy = Policies.CanViewClinical)]
    public async Task<IActionResult> Download(Guid documentId, [FromQuery] bool download = false, CancellationToken ct = default)
    {
        var file = await documents.OpenAsync(documentId, ct);
        if (file is null)
            return NotFound(new ProblemDetails { Title = "Document not found.", Status = 404 });

        var (content, contentType, fileName) = file.Value;

        // Inline for anything the browser can render, attachment otherwise.
        return download || !DocumentService.IsViewableInline(contentType)
            ? File(content, contentType, fileName)
            : File(content, contentType);
    }

    [HttpDelete("documents/{documentId:guid}")]
    [Authorize(Policy = Policies.CanEditClinical)]
    public async Task<IActionResult> Delete(Guid documentId, [FromQuery] string reason = "Removed by staff",
        CancellationToken ct = default)
    {
        var result = await documents.DeleteAsync(documentId, reason, ct);

        return result.Succeeded
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = result.ErrorMessage, Status = 400 });
    }
}
