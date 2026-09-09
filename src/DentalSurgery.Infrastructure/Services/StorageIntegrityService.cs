using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>
/// Checks that the document store and the database still agree.
/// <para>
/// Patient documents live in two places at once: the row is in the database and
/// the file is on disk. Nothing keeps those two in step, and most hosts back
/// them up on different schedules — or back up only the database. A restore can
/// therefore leave a radiograph <em>record</em>, complete with its exposure
/// details and clinical justification, pointing at an image that is gone. That
/// is worse than losing both, because the record asserts the image exists.
/// </para>
/// <para>
/// This makes the disagreement visible. It reads and reports; it never deletes,
/// because both halves of a mismatch may be the half worth keeping and only a
/// person can decide which.
/// </para>
/// </summary>
public class StorageIntegrityService(
    DentalDbContext db,
    IFileStorage storage,
    IPermissionGuard guard,
    ILogger<StorageIntegrityService> logger)
{
    /// <summary>
    /// Compares every stored document against the file it names.
    /// <para>
    /// Run it after a restore, before trusting the restore. Run it periodically
    /// to catch a backup that has been silently covering only one half.
    /// </para>
    /// </summary>
    public async Task<StorageIntegrityReport> CheckAsync(CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DataProtectionReview, ct);

        var documents = await db.PatientDocuments.AsNoTracking()
            .Select(d => new
            {
                d.Id,
                d.PatientId,
                d.Title,
                d.FileName,
                d.StoragePath,
                d.SizeBytes
            })
            .ToListAsync(ct);

        var missing = new List<MissingFile>();
        var checkedCount = 0;

        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();
            checkedCount++;

            if (string.IsNullOrWhiteSpace(document.StoragePath))
            {
                missing.Add(new MissingFile(
                    document.Id, document.PatientId, document.Title, "(no path recorded)"));
                continue;
            }

            if (!storage.Exists(document.StoragePath))
            {
                missing.Add(new MissingFile(
                    document.Id, document.PatientId, document.Title, document.StoragePath));
            }
        }

        // A radiograph is called out separately: the exposure record carries a
        // dose and a clinical justification, so a missing image there is a
        // clinical-governance problem, not just a lost file.
        var radiographIds = await db.RadiographRecords.AsNoTracking()
            .Where(r => r.DocumentId != null)
            .Select(r => r.DocumentId!.Value)
            .ToListAsync(ct);

        var missingRadiographs = missing.Count(m => radiographIds.Contains(m.DocumentId));

        var report = new StorageIntegrityReport(checkedCount, missing, missingRadiographs);

        if (report.IsHealthy)
        {
            logger.LogInformation(
                "Storage integrity check passed: {Count} document(s), all present.", checkedCount);
        }
        else
        {
            logger.LogError(
                "Storage integrity check failed: {Missing} of {Count} document(s) are recorded but " +
                "absent from the store, including {Radiographs} radiograph image(s). The database and " +
                "the document store are out of step - most often because they were backed up or " +
                "restored separately.",
                missing.Count, checkedCount, missingRadiographs);
        }

        return report;
    }
}

/// <summary>The outcome of comparing the database against the document store.</summary>
public record StorageIntegrityReport(
    int DocumentsChecked,
    IReadOnlyList<MissingFile> Missing,
    int MissingRadiographs)
{
    public bool IsHealthy => Missing.Count == 0;
}

/// <summary>A document the database knows about and the store does not have.</summary>
public record MissingFile(Guid DocumentId, Guid PatientId, string Title, string StoragePath);
