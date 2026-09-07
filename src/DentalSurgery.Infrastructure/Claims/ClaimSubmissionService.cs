using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Exporting;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DentalSurgery.Infrastructure.Claims;

/// <summary>What a claim looks like before it is sent.</summary>
public record ClaimPreview(
    string ClaimNumber,
    string Interchange,
    IReadOnlyList<ClaimValidationIssue> Issues,
    string GatewayName,
    bool GatewayConfigured)
{
    public bool CanSubmit => !Issues.Any(i => i.IsBlocking);
    public int SegmentCount => Interchange.Count(c => c == '~');
}

/// <summary>
/// Validates, generates and transmits insurance claims, then records what
/// happened against the claim so the practice has an audit trail of every
/// submission attempt.
/// </summary>
public class ClaimSubmissionService(
    IDbContextFactory<DentalDbContext> dbFactory,
    X12ClaimGenerator generator,
    ClaimGatewaySelector gatewaySelector,
    IPracticeAccessor practice,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    ILogger<ClaimSubmissionService> logger)
{
    /// <summary>Generates the interchange and reports anything that would cause a rejection.</summary>
    public async Task<Result<ClaimPreview>> PrepareAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await LoadAsync(claimId, ct);
        if (claim is null) return Result<ClaimPreview>.Failure("Claim not found.");

        var issues = generator.Validate(claim);
        var gateway = gatewaySelector.Resolve();

        // A claim with blocking problems still gets a preview so the user can
        // see exactly what the payer would have received.
        var interchange = issues.Any(i => i.IsBlocking)
            ? string.Empty
            : generator.Build(claim, practice.Current, clock.UtcNow);

        return Result<ClaimPreview>.Success(new ClaimPreview(
            claim.ClaimNumber, interchange, issues, gateway.Name, gateway.IsConfigured));
    }

    /// <summary>Sends the claim through the configured gateway.</summary>
    public async Task<Result<ClaimSubmissionResult>> SubmitAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await LoadAsync(claimId, ct);
        if (claim is null) return Result<ClaimSubmissionResult>.Failure("Claim not found.");

        if (claim.Status is ClaimStatus.Paid or ClaimStatus.Closed)
            return Result<ClaimSubmissionResult>.Failure("This claim is already settled.");

        var issues = generator.Validate(claim);
        var blocking = issues.Where(i => i.IsBlocking).ToList();

        if (blocking.Count > 0)
            return Result<ClaimSubmissionResult>.Failure(
                blocking.Select(i => $"{i.Field}: {i.Message}"));

        var interchange = generator.Build(claim, practice.Current, clock.UtcNow);
        var gateway = gatewaySelector.Resolve();

        var result = await gateway.SubmitAsync(claim.ClaimNumber, interchange, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tracked = await db.InsuranceClaims.FirstAsync(c => c.Id == claimId, ct);

        if (result.Accepted)
        {
            var isResubmission = tracked.SubmittedOn is not null;
            if (isResubmission) tracked.ResubmissionCount++;

            tracked.Status = ClaimStatus.Submitted;
            tracked.SubmittedOn = clock.Today;
            tracked.SubmissionMethod = result.Method;
            tracked.PayerClaimReference = result.Reference;
            tracked.Notes = Append(tracked.Notes,
                $"{clock.UtcNow:d MMM yyyy HH:mm} submitted via {result.Method}" +
                (result.Reference is null ? "" : $" (reference {result.Reference})") +
                $" by {currentUser.DisplayName ?? "system"}.");

            // Keep the transmitted file against the patient record.
            if (result.StoredAtPath is not null)
            {
                var bytes = Encoding.ASCII.GetByteCount(interchange);
                db.PatientDocuments.Add(new PatientDocument
                {
                    PatientId = tracked.PatientId,
                    DocumentType = DocumentType.InsuranceDocument,
                    Title = $"Claim {tracked.ClaimNumber} interchange",
                    Description = $"837D transmitted via {result.Method}.",
                    FileName = Path.GetFileName(result.StoredAtPath),
                    StoragePath = result.StoredAtPath,
                    ContentType = "application/edi-x12",
                    SizeBytes = bytes,
                    UploadedAtUtc = clock.UtcNow,
                    UploadedBy = currentUser.DisplayName,
                    IsClinicallySignificant = false
                });
            }

            logger.LogInformation("Claim {Claim} submitted via {Method}.", tracked.ClaimNumber, result.Method);
        }
        else
        {
            tracked.Notes = Append(tracked.Notes,
                $"{clock.UtcNow:d MMM yyyy HH:mm} submission failed via {result.Method}: {result.Error}");

            logger.LogError("Claim {Claim} could not be submitted: {Error}", tracked.ClaimNumber, result.Error);
        }

        await db.SaveChangesAsync(ct);

        return result.Accepted
            ? Result<ClaimSubmissionResult>.Success(result)
            : Result<ClaimSubmissionResult>.Failure(result.Error ?? "The submission failed.");
    }

    /// <summary>Produces the interchange for download without transmitting it.</summary>
    public async Task<Result<byte[]>> DownloadInterchangeAsync(Guid claimId, CancellationToken ct = default)
    {
        var preview = await PrepareAsync(claimId, ct);
        if (preview.Failed) return Result<byte[]>.Failure(preview.Errors);

        if (!preview.Value!.CanSubmit)
            return Result<byte[]>.Failure(
                preview.Value.Issues.Where(i => i.IsBlocking).Select(i => $"{i.Field}: {i.Message}"));

        return Result<byte[]>.Success(Encoding.ASCII.GetBytes(preview.Value.Interchange));
    }

    /// <summary>Submits every claim that is ready, reporting each outcome.</summary>
    public async Task<IReadOnlyList<(string ClaimNumber, bool Accepted, string? Error)>> SubmitBatchAsync(
        IEnumerable<Guid> claimIds, CancellationToken ct = default)
    {
        var outcomes = new List<(string, bool, string?)>();

        foreach (var claimId in claimIds)
        {
            var result = await SubmitAsync(claimId, ct);

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var number = await db.InsuranceClaims.Where(c => c.Id == claimId)
                .Select(c => c.ClaimNumber).FirstOrDefaultAsync(ct) ?? claimId.ToString();

            outcomes.Add((number, result.Succeeded, result.Succeeded ? null : result.ErrorMessage));
        }

        return outcomes;
    }

    private async Task<InsuranceClaim?> LoadAsync(Guid claimId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.InsuranceClaims.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.Provider)
            .Include(c => c.PatientInsurance).ThenInclude(i => i!.InsurancePlan).ThenInclude(p => p!.InsuranceCarrier)
            .Include(c => c.Lines).ThenInclude(l => l.ProcedureCode)
            .Include(c => c.Lines).ThenInclude(l => l.Tooth)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);
    }

    private static string Append(string? existing, string line) =>
        string.IsNullOrWhiteSpace(existing) ? line : $"{existing}\n{line}";
}
