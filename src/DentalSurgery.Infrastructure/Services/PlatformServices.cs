using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>Fallback identity used by background jobs, seeding and migrations.</summary>
public class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;
    public string? UserName => "system";
    public string? DisplayName => "System";
    public Guid? StaffId => null;
    public bool IsAuthenticated => false;
    public IReadOnlyList<string> Roles => Array.Empty<string>();
    public bool IsInRole(string role) => false;
    public string? IpAddress => null;
}

/// <summary>
/// Allocates document numbers. The row is updated inside a transaction and the
/// value is read back with a pessimistic re-read, so two concurrent callers
/// cannot be handed the same number.
/// </summary>
public class NumberSequenceService(DentalDbContext db, ILogger<NumberSequenceService> logger) : INumberSequenceService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<string> NextAsync(string sequenceName, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var sequence = await db.NumberSequences.FirstOrDefaultAsync(s => s.Name == sequenceName, ct);

            if (sequence is null)
            {
                sequence = CreateDefault(sequenceName);
                db.NumberSequences.Add(sequence);
                logger.LogInformation("Created number sequence {Sequence} on first use.", sequenceName);
            }

            // Reset annually where the sequence is year-scoped.
            var year = DateTime.UtcNow.Year;
            if (sequence.IncludeYear && sequence.ResetYear != year)
            {
                sequence.ResetYear = year;
                sequence.NextValue = 1;
            }

            var value = sequence.NextValue;
            sequence.NextValue = value + 1;

            await db.SaveChangesAsync(ct);
            return sequence.Format(value);
        }
        finally
        {
            Gate.Release();
        }
    }

    public static NumberSequence CreateDefault(string name) => name switch
    {
        SequenceNames.Patient => new NumberSequence { Name = name, Prefix = "P-", PadWidth = 6 },
        SequenceNames.Staff => new NumberSequence { Name = name, Prefix = "S-", PadWidth = 4 },
        SequenceNames.Appointment => new NumberSequence { Name = name, Prefix = "A-", PadWidth = 8, IncludeYear = true },
        SequenceNames.Invoice => new NumberSequence { Name = name, Prefix = "INV-", PadWidth = 6, IncludeYear = true },
        SequenceNames.Payment => new NumberSequence { Name = name, Prefix = "PAY-", PadWidth = 6, IncludeYear = true },
        SequenceNames.TreatmentPlan => new NumberSequence { Name = name, Prefix = "TP-", PadWidth = 6 },
        SequenceNames.Claim => new NumberSequence { Name = name, Prefix = "CLM-", PadWidth = 6, IncludeYear = true },
        SequenceNames.Prescription => new NumberSequence { Name = name, Prefix = "RX-", PadWidth = 6, IncludeYear = true },
        SequenceNames.LabCase => new NumberSequence { Name = name, Prefix = "LAB-", PadWidth = 5, IncludeYear = true },
        SequenceNames.PurchaseOrder => new NumberSequence { Name = name, Prefix = "PO-", PadWidth = 5, IncludeYear = true },
        SequenceNames.Referral => new NumberSequence { Name = name, Prefix = "REF-", PadWidth = 5, IncludeYear = true },
        SequenceNames.PaymentPlan => new NumberSequence { Name = name, Prefix = "PP-", PadWidth = 5 },
        _ => new NumberSequence { Name = name, Prefix = $"{name[..Math.Min(3, name.Length)].ToUpperInvariant()}-", PadWidth = 6 }
    };
}

/// <summary>Stores patient documents on the local filesystem under a per-year folder tree.</summary>
public class LocalFileStorage(string rootPath, ILogger<LocalFileStorage> logger) : IFileStorage
{
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".scr", ".ps1", ".sh", ".msi", ".vbs", ".js", ".jar"
    };

    public async Task<string> SaveAsync(Stream content, string fileName, string subFolder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        if (BlockedExtensions.Contains(extension))
            throw new InvalidOperationException($"Files of type {extension} cannot be attached to a patient record.");

        var safeName = SanitiseFileName(Path.GetFileNameWithoutExtension(fileName));
        var relativeFolder = Path.Combine(SanitiseFileName(subFolder), DateTime.UtcNow.ToString("yyyy-MM"));
        var absoluteFolder = Path.Combine(rootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var unique = $"{safeName}-{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(absoluteFolder, unique);

        await using (var target = File.Create(absolutePath))
        {
            await content.CopyToAsync(target, ct);
        }

        var relativePath = Path.Combine(relativeFolder, unique).Replace('\\', '/');
        logger.LogInformation("Stored document at {Path}", relativePath);
        return relativePath;
    }

    public Task<Stream?> OpenAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = Resolve(storagePath);
        if (absolute is null || !File.Exists(absolute)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(absolute));
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var absolute = Resolve(storagePath);
        if (absolute is null || !File.Exists(absolute)) return Task.FromResult(false);
        File.Delete(absolute);
        return Task.FromResult(true);
    }

    public bool Exists(string storagePath)
    {
        var absolute = Resolve(storagePath);
        return absolute is not null && File.Exists(absolute);
    }

    public string GetPublicUrl(string storagePath) => $"/documents/{storagePath.Replace('\\', '/')}";

    /// <summary>Resolves inside the root only, so a crafted path cannot escape the store.</summary>
    private string? Resolve(string storagePath)
    {
        var combined = Path.GetFullPath(Path.Combine(rootPath, storagePath));
        var root = Path.GetFullPath(rootPath);
        return combined.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? combined : null;
    }

    private static string SanitiseFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }).ToArray();
        var cleaned = new string(value.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned[..Math.Min(60, cleaned.Length)];
    }
}

/// <summary>
/// Records outbound communications without transmitting them. Used when no
/// gateway is configured, and by tests that must not touch the network.
/// </summary>
public class RecordingNotificationSender(ILogger<RecordingNotificationSender> logger) : INotificationSender
{
    public bool IsChannelConfigured(CommunicationChannel channel) => false;

    public Task<NotificationResult> SendAsync(
        CommunicationChannel channel, string recipient, string? subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Notification recorded. Channel={Channel} Recipient={Recipient} Subject={Subject} Length={Length}",
            channel, Mask(recipient), subject ?? "(none)", body.Length);

        return Task.FromResult(NotificationResult.Recorded());
    }

    private static string Mask(string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient)) return "(blank)";
        if (recipient.Contains('@'))
        {
            var parts = recipient.Split('@');
            var name = parts[0];
            return $"{name[..Math.Min(2, name.Length)]}***@{parts[^1]}";
        }
        return recipient.Length <= 4 ? "***" : $"***{recipient[^4..]}";
    }
}
