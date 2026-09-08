using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>A file the caller wants attached to a patient record.</summary>
public record UploadRequest(
    Guid PatientId,
    Stream Content,
    string FileName,
    string DeclaredContentType,
    long Length,
    DocumentType DocumentType,
    string? Title = null,
    string? Description = null,
    Guid? AppointmentId = null,
    Guid? ProcedureId = null,
    bool ClinicallySignificant = false,
    bool PatientVisible = false);

/// <summary>Extra detail when the upload is a radiograph or clinical image.</summary>
public record RadiographUploadRequest(
    RadiographType RadiographType,
    string? ToothNumbers = null,
    decimal? KiloVoltagePeak = null,
    decimal? MilliAmperage = null,
    decimal? ExposureSeconds = null,
    decimal? DoseMicroSieverts = null,
    string? JustificationReason = null,
    string? QualityRating = null,
    string? Findings = null,
    bool IsRepeat = false,
    string? RepeatReason = null,
    Guid? TakenByStaffId = null);

/// <summary>
/// Stores and serves patient documents and images. Uploads are checked by
/// content rather than trusting the declared type or the extension, hashed for
/// integrity, and recorded against the patient.
/// </summary>
public class DocumentService(
    IDbContextFactory<DentalDbContext> dbFactory,
    IFileStorage storage,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<DocumentService> logger,
    IPermissionGuard guard)
{
    public const long MaxUploadBytes = 40 * 1024 * 1024;

    /// <summary>Types the practice accepts, keyed by the magic bytes that identify them.</summary>
    private static readonly (byte[] Signature, string ContentType, string Extension)[] Signatures =
    {
        (new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf", ".pdf"),
        (new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg", ".jpg"),
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png", ".png"),
        (new byte[] { 0x47, 0x49, 0x46, 0x38 }, "image/gif", ".gif"),
        (new byte[] { 0x42, 0x4D }, "image/bmp", ".bmp"),
        (new byte[] { 0x49, 0x49, 0x2A, 0x00 }, "image/tiff", ".tif"),
        (new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, "image/tiff", ".tif"),
        (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "application/zip", ".zip")
    };

    /// <summary>DICOM carries its marker 128 bytes in rather than at the start.</summary>
    private static readonly byte[] DicomMarker = { 0x44, 0x49, 0x43, 0x4D };

    public async Task<Result<PatientDocument>> UploadAsync(UploadRequest request, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DocumentsUpload, ct);
        if (request.Length <= 0)
            return Result<PatientDocument>.Failure("The file is empty.");

        if (request.Length > MaxUploadBytes)
            return Result<PatientDocument>.Failure(
                $"The file is {request.Length / 1024 / 1024} MB. The limit is {MaxUploadBytes / 1024 / 1024} MB.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var patientExists = await db.Patients.AnyAsync(p => p.Id == request.PatientId, ct);
        if (!patientExists) return Result<PatientDocument>.Failure("Patient not found.");

        // Buffer so the content can be sniffed, hashed and written.
        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var detected = Detect(buffer);
        if (detected is null)
            return Result<PatientDocument>.Failure(
                "The file type could not be recognised. Accepted types are PDF, JPEG, PNG, GIF, BMP, TIFF, DICOM and ZIP.");

        var (contentType, extension) = detected.Value;

        buffer.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(buffer, ct));

        // The same file uploaded twice is stored once and reported as a duplicate.
        var duplicate = await db.PatientDocuments
            .FirstOrDefaultAsync(d => d.PatientId == request.PatientId && d.Sha256 == hash, ct);

        if (duplicate is not null)
        {
            logger.LogInformation("Upload for patient {PatientId} matched existing document {DocumentId}.",
                request.PatientId, duplicate.Id);
            return Result<PatientDocument>.Success(duplicate);
        }

        buffer.Position = 0;
        var storedName = Path.ChangeExtension(SafeStem(request.FileName), extension.TrimStart('.'));
        var storagePath = await storage.SaveAsync(buffer, storedName, $"patients/{request.PatientId:N}", ct);

        var document = new PatientDocument
        {
            PatientId = request.PatientId,
            DocumentType = request.DocumentType,
            Title = string.IsNullOrWhiteSpace(request.Title)
                ? Path.GetFileNameWithoutExtension(request.FileName)
                : request.Title,
            Description = request.Description,
            FileName = Path.GetFileName(request.FileName),
            StoragePath = storagePath,
            ContentType = contentType,
            SizeBytes = buffer.Length,
            Sha256 = hash,
            UploadedAtUtc = clock.UtcNow,
            UploadedBy = currentUser.DisplayName ?? currentUser.UserName,
            AppointmentId = request.AppointmentId,
            ProcedureId = request.ProcedureId,
            IsClinicallySignificant = request.ClinicallySignificant,
            IsPatientVisible = request.PatientVisible
        };

        db.PatientDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Stored {Type} document {DocumentId} ({Bytes} bytes) for patient {PatientId}.",
            contentType, document.Id, document.SizeBytes, request.PatientId);

        return Result<PatientDocument>.Success(document);
    }

    /// <summary>Uploads an image and records the radiographic exposure alongside it.</summary>
    public async Task<Result<RadiographRecord>> UploadRadiographAsync(
        UploadRequest upload, RadiographUploadRequest details, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.ImagingCreate, ct);
        var stored = await UploadAsync(upload with { DocumentType = DocumentType.Radiograph }, ct);
        if (stored.Failed) return Result<RadiographRecord>.Failure(stored.Errors);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var record = new RadiographRecord
        {
            PatientId = upload.PatientId,
            RadiographType = details.RadiographType,
            TakenAtUtc = clock.UtcNow,
            TakenByStaffId = details.TakenByStaffId ?? currentUser.StaffId,
            AppointmentId = upload.AppointmentId,
            ToothNumbers = details.ToothNumbers,
            KiloVoltagePeak = details.KiloVoltagePeak,
            MilliAmperage = details.MilliAmperage,
            ExposureSeconds = details.ExposureSeconds,
            DoseMicroSieverts = details.DoseMicroSieverts,
            JustificationReason = details.JustificationReason,
            QualityRating = details.QualityRating,
            Findings = details.Findings,
            IsRepeat = details.IsRepeat,
            RepeatReason = details.RepeatReason,
            LeadApronUsed = false,
            DocumentId = stored.Value!.Id,
            ImagePath = stored.Value.StoragePath
        };

        db.RadiographRecords.Add(record);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Recorded {Type} radiograph {RecordId} for patient {PatientId}.",
            details.RadiographType, record.Id, upload.PatientId);

        return Result<RadiographRecord>.Success(record);
    }

    /// <summary>
    /// Opens a document belonging to a named patient, or null when there is no
    /// such document on that patient.
    /// <para>
    /// The patient is a parameter rather than something read from the document,
    /// so the caller has to say whose record it believes it is opening and the
    /// two have to agree. That is what stops a guessed or copied document
    /// identifier from yielding a different patient's file: the identifier
    /// alone is not enough, and a mismatch is refused and recorded rather than
    /// quietly served.
    /// </para>
    /// </summary>
    public async Task<(Stream Content, string ContentType, string FileName)?> OpenAsync(
        Guid patientId, Guid documentId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DocumentsView, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var document = await db.PatientDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (document is null) return null;

        if (document.PatientId != patientId)
        {
            // Logged as a security event. A legitimate interface never produces
            // this: it means an identifier arrived from somewhere other than the
            // patient's own record.
            logger.LogWarning(
                "Refused document {DocumentId}: requested under patient {RequestedPatientId} " +
                "but it belongs to {OwningPatientId}. Requested by {User}.",
                documentId, patientId, document.PatientId, currentUser.UserName ?? "(unknown)");

            // Answered as "not found" rather than "forbidden", so the response
            // does not confirm that the identifier exists on some other patient.
            return null;
        }

        // Every read of a patient document is recorded, not just every change.
        // Who looked at what is the question an information-governance review
        // actually asks, and it cannot be answered retrospectively.
        logger.LogInformation(
            "Document {DocumentId} for patient {PatientId} opened by {User}.",
            documentId, document.PatientId, currentUser.UserName ?? "(unknown)");

        var stream = await storage.OpenAsync(document.StoragePath, ct);
        if (stream is null)
        {
            logger.LogWarning("Document {DocumentId} is recorded but its file is missing at {Path}.",
                documentId, document.StoragePath);
            return null;
        }

        return (stream, document.ContentType, document.FileName);
    }

    /// <summary>
    /// Removes a document. The record is soft-deleted so the audit trail keeps
    /// the fact it existed; the file itself is removed to honour erasure requests.
    /// </summary>
    public async Task<Result> DeleteAsync(
        Guid patientId, Guid documentId, string reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DocumentsDelete, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var document = await db.PatientDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (document is null) return Result.Failure("Document not found.");

        if (document.PatientId != patientId)
        {
            logger.LogWarning(
                "Refused deletion of document {DocumentId}: requested under patient {RequestedPatientId} " +
                "but it belongs to {OwningPatientId}. Requested by {User}.",
                documentId, patientId, document.PatientId, currentUser.UserName ?? "(unknown)");

            return Result.Failure("Document not found.");
        }

        var linkedRadiograph = await db.RadiographRecords
            .AnyAsync(r => r.DocumentId == documentId, ct);

        if (linkedRadiograph)
            return Result.Failure(
                "This image is attached to a radiograph record and forms part of the clinical record.");

        document.Description = string.IsNullOrWhiteSpace(document.Description)
            ? $"Deleted: {reason}"
            : $"{document.Description}\nDeleted: {reason}";

        db.PatientDocuments.Remove(document);
        await db.SaveChangesAsync(ct);

        await storage.DeleteAsync(document.StoragePath, ct);

        logger.LogInformation("Deleted document {DocumentId}: {Reason}", documentId, reason);
        return Result.Success();
    }

    public async Task<List<PatientDocument>> ListAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DocumentsView, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.PatientDocuments.AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .ToListAsync(ct);
    }

    /// <summary>True when the browser can display the file inline.</summary>
    public static bool IsViewableInline(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Identifies the file from its own bytes. A renamed executable is rejected
    /// here rather than being trusted because of its extension.
    /// </summary>
    private static (string ContentType, string Extension)? Detect(Stream stream)
    {
        Span<byte> header = stackalloc byte[132];
        stream.Position = 0;
        var read = stream.Read(header);
        stream.Position = 0;

        if (read < 4) return null;

        foreach (var (signature, contentType, extension) in Signatures)
        {
            if (read < signature.Length) continue;
            if (header[..signature.Length].SequenceEqual(signature)) return (contentType, extension);
        }

        // DICOM: "DICM" at offset 128.
        if (read >= 132 && header[128..132].SequenceEqual(DicomMarker))
            return ("application/dicom", ".dcm");

        return null;
    }

    private static string SafeStem(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(stem.Where(c => !invalid.Contains(c) && c != ' ').ToArray()).Trim('.', '-');
        return string.IsNullOrWhiteSpace(cleaned) ? "document" : cleaned[..Math.Min(60, cleaned.Length)];
    }
}
