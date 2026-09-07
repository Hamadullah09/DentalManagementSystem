using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace DentalSurgery.Infrastructure.Claims;

/// <summary>
/// Writes the interchange to a folder the clearing house collects from. This is
/// how most practices actually submit: the clearing house agent or an SFTP job
/// picks the file up, and the archive copy is the practice's own record.
/// </summary>
public class FileDropClaimGateway(
    IOptions<ClaimSubmissionOptions> options,
    ILogger<FileDropClaimGateway> logger) : IClaimSubmissionGateway
{
    public string Name => "File drop";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.OutboundFolder);

    public async Task<ClaimSubmissionResult> SubmitAsync(
        string claimNumber, string interchange, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return ClaimSubmissionResult.Failure(Name, "No outbound folder is configured.");

        try
        {
            var outbound = Resolve(options.Value.OutboundFolder);
            var archive = Resolve(options.Value.ArchiveFolder);
            Directory.CreateDirectory(outbound);
            Directory.CreateDirectory(archive);

            var fileName = $"{Safe(claimNumber)}-{DateTime.UtcNow:yyyyMMddHHmmss}.837";
            var outboundPath = Path.Combine(outbound, fileName);
            var archivePath = Path.Combine(archive, fileName);

            var bytes = Encoding.ASCII.GetBytes(interchange);
            await File.WriteAllBytesAsync(outboundPath, bytes, ct);
            await File.WriteAllBytesAsync(archivePath, bytes, ct);

            logger.LogInformation("Claim {Claim} written to {Path} ({Bytes} bytes).",
                claimNumber, outboundPath, bytes.Length);

            return ClaimSubmissionResult.Success(Name, fileName, archivePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not write claim {Claim} to the outbound folder.", claimNumber);
            return ClaimSubmissionResult.Failure(Name, ex.Message);
        }
    }

    private static string Resolve(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static string Safe(string value) =>
        new(value.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-').ToArray());
}

/// <summary>Posts the interchange to a clearing house HTTP endpoint.</summary>
public class HttpClaimGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<ClaimSubmissionOptions> options,
    ILogger<HttpClaimGateway> logger) : IClaimSubmissionGateway
{
    public string Name => "Clearing house";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.Endpoint);

    public async Task<ClaimSubmissionResult> SubmitAsync(
        string claimNumber, string interchange, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return ClaimSubmissionResult.Failure(Name, "No clearing house endpoint is configured.");

        try
        {
            var client = httpClientFactory.CreateClient("claims");
            client.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.Endpoint)
            {
                Content = new StringContent(interchange, Encoding.ASCII, "application/edi-x12")
            };

            if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);

            request.Headers.Add("X-Claim-Number", claimNumber);

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Clearing house rejected claim {Claim}: {Status} {Body}",
                    claimNumber, (int)response.StatusCode, Truncate(body));
                return ClaimSubmissionResult.Failure(Name,
                    $"The clearing house returned {(int)response.StatusCode}: {Truncate(body)}");
            }

            logger.LogInformation("Claim {Claim} accepted by the clearing house.", claimNumber);
            return ClaimSubmissionResult.Success(Name, Truncate(body, 64));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Submission of claim {Claim} failed.", claimNumber);
            return ClaimSubmissionResult.Failure(Name, ex.Message);
        }
    }

    private static string Truncate(string value, int length = 200) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= length ? value.Trim() : value[..length].Trim();
}

/// <summary>Used when the practice submits on paper or through a payer portal.</summary>
public class ManualClaimGateway(ILogger<ManualClaimGateway> logger) : IClaimSubmissionGateway
{
    public string Name => "Manual";
    public bool IsConfigured => true;

    public Task<ClaimSubmissionResult> SubmitAsync(
        string claimNumber, string interchange, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Claim {Claim} generated for manual submission ({Bytes} bytes). Download the file or the claim form.",
            claimNumber, interchange.Length);

        return Task.FromResult(ClaimSubmissionResult.Success(Name, "Generated for manual submission"));
    }
}

/// <summary>Chooses the gateway named in configuration.</summary>
public class ClaimGatewaySelector(
    FileDropClaimGateway fileDrop,
    HttpClaimGateway http,
    ManualClaimGateway manual,
    IOptions<ClaimSubmissionOptions> options)
{
    public IClaimSubmissionGateway Resolve() => options.Value.Mode?.ToLowerInvariant() switch
    {
        "http" => http.IsConfigured ? http : manual,
        "filedrop" => fileDrop.IsConfigured ? fileDrop : manual,
        _ => manual
    };
}
