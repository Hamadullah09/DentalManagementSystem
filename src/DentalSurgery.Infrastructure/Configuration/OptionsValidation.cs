using DentalSurgery.Infrastructure.Notifications;
using DentalSurgery.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DentalSurgery.Infrastructure.Configuration;

/// <summary>
/// Start-up validation for the configuration that carries credentials or
/// identifies the practice to an outside party.
/// <para>
/// These run through <c>ValidateOnStart</c>, so a misconfigured deployment
/// fails immediately with one clear message. The alternative is a host that
/// starts cleanly and then fails per-request, hours later, in whichever
/// screen happens to touch the broken integration first.
/// </para>
/// <para>
/// The rule throughout is that a channel switched <em>on</em> must be complete.
/// A channel switched off is never validated, because "not configured" is a
/// supported, documented state that the application reports honestly.
/// </para>
/// </summary>
public class NotificationOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<NotificationOptions>
{
    public ValidateOptionsResult Validate(string? name, NotificationOptions options)
    {
        var failures = new List<string>();

        ValidateEmail(options.Email, failures);
        ValidateSms(options.Sms, failures);

        if (options.DispatchIntervalMinutes < 1)
            failures.Add("Notifications:DispatchIntervalMinutes must be at least 1.");

        if (options.MaxAttempts < 1)
            failures.Add("Notifications:MaxAttempts must be at least 1.");

        if (options.AbandonAfterHours < 1)
            failures.Add("Notifications:AbandonAfterHours must be at least 1.");

        // A copy of live data outside production must not message real patients.
        // This cannot be enforced from here, but the combination worth warning
        // about is a non-production host with outbound enabled and no redirect.
        if (!environment.IsProduction()
            && (options.Email.IsUsable || options.Sms.IsUsable)
            && !options.SuppressOutbound
            && string.IsNullOrWhiteSpace(options.RedirectAllTo))
        {
            failures.Add(
                $"A messaging gateway is enabled in the {environment.EnvironmentName} environment with no " +
                "Notifications:RedirectAllTo and Notifications:SuppressOutbound off. A non-production " +
                "instance holding a copy of live data would message real patients. Set one of them.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateEmail(EmailOptions email, List<string> failures)
    {
        if (!email.Enabled) return;

        if (string.IsNullOrWhiteSpace(email.Host))
            failures.Add("Notifications:Email is enabled but Host is not set.");

        if (string.IsNullOrWhiteSpace(email.FromAddress))
            failures.Add("Notifications:Email is enabled but FromAddress is not set.");

        // A username with no password means the send fails at authentication,
        // which surfaces as an unexplained bounce rather than a config error.
        if (!string.IsNullOrWhiteSpace(email.Username) && string.IsNullOrWhiteSpace(email.Password))
        {
            failures.Add(
                "Notifications:Email has a Username but no Password. Supply it through an environment " +
                "variable or secret store (Notifications__Email__Password), not appsettings.json.");
        }

        if (email.Port is < 1 or > 65535)
            failures.Add("Notifications:Email:Port must be between 1 and 65535.");

        if (!email.UseStartTls && !email.UseSsl)
        {
            failures.Add(
                "Notifications:Email has both UseStartTls and UseSsl off, which would send patient " +
                "correspondence and credentials over an unencrypted connection.");
        }
    }

    private static void ValidateSms(SmsOptions sms, List<string> failures)
    {
        if (!sms.Enabled) return;

        if (string.IsNullOrWhiteSpace(sms.Endpoint))
            failures.Add("Notifications:Sms is enabled but Endpoint is not set.");
        else if (!Uri.TryCreate(sms.Endpoint, UriKind.Absolute, out var uri)
                 || uri.Scheme != Uri.UriSchemeHttps)
            failures.Add("Notifications:Sms:Endpoint must be an absolute https URL.");

        if (string.IsNullOrWhiteSpace(sms.FromNumber))
            failures.Add("Notifications:Sms is enabled but FromNumber is not set.");

        var hasBasic = !string.IsNullOrWhiteSpace(sms.AccountSid) && !string.IsNullOrWhiteSpace(sms.AuthToken);
        var hasBearer = !string.IsNullOrWhiteSpace(sms.BearerToken);

        if (!hasBasic && !hasBearer)
        {
            failures.Add(
                "Notifications:Sms is enabled but has no credentials. Set either AccountSid and AuthToken " +
                "or BearerToken, through an environment variable or secret store.");
        }
    }
}

/// <summary>
/// Validates claim submission. The identifiers here are what a payer uses to
/// decide whose claim it is, so a transmitting instance must have real ones.
/// </summary>
public class ClaimSubmissionOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<ClaimSubmissionOptions>
{
    private static readonly string[] Transmitting = ["FileDrop", "Http"];

    public ValidateOptionsResult Validate(string? name, ClaimSubmissionOptions options)
    {
        var failures = new List<string>();
        var mode = options.Mode?.Trim() ?? string.Empty;

        if (!IsKnownMode(mode))
        {
            failures.Add($"ClaimSubmission:Mode '{options.Mode}' is not recognised. " +
                         "Use Manual, FileDrop, Http or None.");
            return ValidateOptionsResult.Fail(failures);
        }

        // Manual and None produce a file for someone to handle; nothing leaves
        // the host, so the interchange identifiers are not required yet.
        var transmits = Transmitting.Contains(mode, StringComparer.OrdinalIgnoreCase);

        if (mode.Equals("Http", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Endpoint))
                failures.Add("ClaimSubmission:Mode is Http but Endpoint is not set.");
            else if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri)
                     || uri.Scheme != Uri.UriSchemeHttps)
                failures.Add("ClaimSubmission:Endpoint must be an absolute https URL. Claims carry " +
                             "patient identifiers and must not cross an unencrypted connection.");

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                failures.Add(
                    "ClaimSubmission:Mode is Http but ApiKey is not set. Supply it through an " +
                    "environment variable or secret store (ClaimSubmission__ApiKey).");
            }
        }

        if (mode.Equals("FileDrop", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(options.OutboundFolder))
        {
            failures.Add("ClaimSubmission:Mode is FileDrop but OutboundFolder is not set.");
        }

        if (transmits)
        {
            RequireIdentifier(options.SubmitterId, "SubmitterId", failures);
            RequireIdentifier(options.SubmitterName, "SubmitterName", failures);
            RequireIdentifier(options.ReceiverId, "ReceiverId", failures);
            RequireIdentifier(options.InterchangeSenderId, "InterchangeSenderId", failures);
            RequireIdentifier(options.InterchangeReceiverId, "InterchangeReceiverId", failures);
            RequireIdentifier(options.BillingProviderId, "BillingProviderId", failures);
        }

        var usage = options.UsageIndicator?.Trim().ToUpperInvariant();
        if (usage is not ("T" or "P"))
        {
            failures.Add("ClaimSubmission:UsageIndicator must be 'T' for test or 'P' for production.");
        }
        else if (usage == "P" && !environment.IsProduction())
        {
            failures.Add(
                $"ClaimSubmission:UsageIndicator is 'P' in the {environment.EnvironmentName} environment. " +
                "A non-production instance would submit live claims against real patient accounts.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsKnownMode(string mode) =>
        mode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
        || mode.Equals("FileDrop", StringComparison.OrdinalIgnoreCase)
        || mode.Equals("Http", StringComparison.OrdinalIgnoreCase)
        || mode.Equals("None", StringComparison.OrdinalIgnoreCase);

    private static void RequireIdentifier(string value, string key, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"ClaimSubmission:{key} must be set before claims can be transmitted. " +
                         "It comes from the clearing house's companion guide.");
        }
    }
}

/// <summary>Validates the seeding policy that governs which accounts exist.</summary>
public class SeedOptionsValidator(IHostEnvironment environment) : IValidateOptions<SeedOptions>
{
    public ValidateOptionsResult Validate(string? name, SeedOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.AdminEmail))
        {
            failures.Add("Seed:AdminEmail must be set.");
        }
        else if (!options.AdminEmail.Contains('@', StringComparison.Ordinal))
        {
            failures.Add($"Seed:AdminEmail '{options.AdminEmail}' is not an email address.");
        }

        // The password itself is checked by the initialiser, which is the only
        // place that knows whether an administrator already exists. What is
        // worth refusing here is a password weak enough that Identity would
        // reject it, discovered at start-up rather than mid-seed.
        if (!string.IsNullOrWhiteSpace(options.AdminPassword) && options.AdminPassword.Length < 10)
        {
            failures.Add("Seed:AdminPassword must be at least 10 characters, matching the practice " +
                         "password policy.");
        }

        if (options.AllowDemoDataOutsideDevelopment && environment.IsProduction())
        {
            failures.Add(
                "Seed:AllowDemoDataOutsideDevelopment is set in Production. Demonstration data creates " +
                "staff logins on a shared, publicly documented password and must never be enabled on an " +
                "instance that holds real patient records.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
