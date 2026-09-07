using DentalSurgery.Infrastructure;
using DentalSurgery.Infrastructure.Configuration;
using DentalSurgery.Infrastructure.Notifications;
using DentalSurgery.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DentalSurgery.Tests;

/// <summary>
/// The configuration defaults and validators that decide whether a deployment
/// is safe. These are the checks that stop a misconfigured instance starting,
/// so they are worth testing directly rather than trusting by inspection.
/// </summary>
public class ConfigurationSecurityTests
{
    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static FakeEnvironment Production => new("Production");
    private static FakeEnvironment Development => new("Development");
    private static FakeEnvironment Staging => new("Staging");

    // ------------------------------------------------------------ seed defaults

    [Fact]
    public void Demonstration_data_is_off_unless_asked_for()
    {
        var options = new SeedOptions();

        Assert.False(options.DemoData);
        Assert.False(options.AllowDemoDataOutsideDevelopment);
    }

    [Fact]
    public void There_is_no_built_in_administrator_password()
    {
        // A default here would become the password of every deployment that
        // did not override it.
        Assert.Null(new SeedOptions().AdminPassword);
    }

    [Fact]
    public void Migrations_do_not_run_on_startup_by_default()
    {
        // Several hosts starting together would otherwise race on the schema.
        var options = new SeedOptions();

        Assert.False(options.MigrateOnStartup);
        Assert.True(options.VerifySchemaOnStartup);
    }

    [Fact]
    public void An_absent_seed_section_yields_the_safe_defaults()
    {
        // Binding an empty configuration must not turn anything on.
        var configuration = new ConfigurationBuilder().Build();
        var options = new SeedOptions();
        configuration.GetSection(SeedOptions.SectionName).Bind(options);

        Assert.False(options.DemoData);
        Assert.Null(options.AdminPassword);
        Assert.False(options.MigrateOnStartup);
    }

    // ------------------------------------------------------------ seed validation

    [Fact]
    public void Demonstration_data_is_refused_in_production()
    {
        var validator = new SeedOptionsValidator(Production);

        var result = validator.Validate(null, new SeedOptions
        {
            DemoData = true,
            AllowDemoDataOutsideDevelopment = true
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Production"));
    }

    [Fact]
    public void Demonstration_data_is_permitted_in_development()
    {
        var validator = new SeedOptionsValidator(Development);

        var result = validator.Validate(null, new SeedOptions { DemoData = true });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void A_short_administrator_password_is_refused()
    {
        var validator = new SeedOptionsValidator(Production);

        var result = validator.Validate(null, new SeedOptions { AdminPassword = "short" });

        Assert.True(result.Failed);
    }

    // ------------------------------------------------------------ messaging

    [Fact]
    public void An_enabled_email_gateway_without_a_host_is_refused()
    {
        var validator = new NotificationOptionsValidator(Production);

        var result = validator.Validate(null, new NotificationOptions
        {
            Email = new EmailOptions { Enabled = true, FromAddress = "a@b.example" }
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Host"));
    }

    [Fact]
    public void A_username_without_a_password_is_refused()
    {
        var validator = new NotificationOptionsValidator(Production);

        var result = validator.Validate(null, new NotificationOptions
        {
            Email = new EmailOptions
            {
                Enabled = true,
                Host = "smtp.example",
                FromAddress = "a@b.example",
                Username = "practice"
            }
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Password"));
    }

    [Fact]
    public void An_unencrypted_smtp_connection_is_refused()
    {
        var validator = new NotificationOptionsValidator(Production);

        var result = validator.Validate(null, new NotificationOptions
        {
            Email = new EmailOptions
            {
                Enabled = true,
                Host = "smtp.example",
                FromAddress = "a@b.example",
                UseStartTls = false,
                UseSsl = false
            }
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void An_sms_gateway_without_credentials_is_refused()
    {
        var validator = new NotificationOptionsValidator(Production);

        var result = validator.Validate(null, new NotificationOptions
        {
            Sms = new SmsOptions
            {
                Enabled = true,
                Endpoint = "https://sms.example/send",
                FromNumber = "+441234567890"
            }
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("credentials"));
    }

    [Fact]
    public void An_sms_endpoint_must_be_https()
    {
        var validator = new NotificationOptionsValidator(Production);

        var result = validator.Validate(null, new NotificationOptions
        {
            Sms = new SmsOptions
            {
                Enabled = true,
                Endpoint = "http://sms.example/send",
                FromNumber = "+441234567890",
                BearerToken = "token"
            }
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("https"));
    }

    [Fact]
    public void A_non_production_instance_may_not_message_real_patients()
    {
        var validator = new NotificationOptionsValidator(Staging);

        var result = validator.Validate(null, new NotificationOptions
        {
            Email = new EmailOptions
            {
                Enabled = true,
                Host = "smtp.example",
                FromAddress = "a@b.example"
            }
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("RedirectAllTo"));
    }

    [Fact]
    public void A_redirected_non_production_instance_is_allowed()
    {
        var validator = new NotificationOptionsValidator(Staging);

        var result = validator.Validate(null, new NotificationOptions
        {
            RedirectAllTo = "staging@practice.example",
            Email = new EmailOptions
            {
                Enabled = true,
                Host = "smtp.example",
                FromAddress = "a@b.example"
            }
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Messaging_that_is_switched_off_is_never_validated()
    {
        // "Not configured" is a supported state the application reports plainly.
        var validator = new NotificationOptionsValidator(Production);

        var result = validator.Validate(null, new NotificationOptions());

        Assert.True(result.Succeeded);
    }

    // ------------------------------------------------------------ claims

    [Fact]
    public void Claim_identifiers_have_no_plausible_defaults()
    {
        // A default NPI would be transmitted on real claims by any deployment
        // that had not configured its own.
        var options = new ClaimSubmissionOptions();

        Assert.Equal(string.Empty, options.BillingProviderId);
        Assert.Equal(string.Empty, options.SubmitterId);
        Assert.Equal(string.Empty, options.InterchangeSenderId);
    }

    [Fact]
    public void Claim_submission_defaults_to_manual()
    {
        Assert.Equal("Manual", new ClaimSubmissionOptions().Mode);
    }

    [Fact]
    public void Transmitting_without_identifiers_is_refused()
    {
        var validator = new ClaimSubmissionOptionsValidator(Production);

        var result = validator.Validate(null, new ClaimSubmissionOptions
        {
            Mode = "FileDrop",
            OutboundFolder = "out"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("BillingProviderId"));
    }

    [Fact]
    public void Manual_submission_does_not_require_identifiers()
    {
        // Nothing leaves the host, so the interchange identifiers are not
        // needed until a transport is configured.
        var validator = new ClaimSubmissionOptionsValidator(Production);

        var result = validator.Validate(null, new ClaimSubmissionOptions { Mode = "Manual" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void An_http_claim_gateway_requires_an_api_key()
    {
        var validator = new ClaimSubmissionOptionsValidator(Production);

        var result = validator.Validate(null, new ClaimSubmissionOptions
        {
            Mode = "Http",
            Endpoint = "https://clearing.example/837",
            SubmitterId = "PRACTICE",
            SubmitterName = "PRACTICE",
            ReceiverId = "CLEARING",
            InterchangeSenderId = "PRACTICE",
            InterchangeReceiverId = "CLEARING",
            BillingProviderId = "1234567893"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("ApiKey"));
    }

    [Fact]
    public void A_claim_endpoint_must_be_https()
    {
        var validator = new ClaimSubmissionOptionsValidator(Production);

        var result = validator.Validate(null, new ClaimSubmissionOptions
        {
            Mode = "Http",
            Endpoint = "http://clearing.example/837",
            ApiKey = "key",
            SubmitterId = "PRACTICE",
            SubmitterName = "PRACTICE",
            ReceiverId = "CLEARING",
            InterchangeSenderId = "PRACTICE",
            InterchangeReceiverId = "CLEARING",
            BillingProviderId = "1234567893"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("https"));
    }

    [Fact]
    public void Live_claims_are_refused_outside_production()
    {
        var validator = new ClaimSubmissionOptionsValidator(Staging);

        var result = validator.Validate(null, new ClaimSubmissionOptions
        {
            Mode = "Manual",
            UsageIndicator = "P"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("live claims"));
    }

    [Fact]
    public void An_unknown_claim_mode_is_refused()
    {
        var validator = new ClaimSubmissionOptionsValidator(Production);

        var result = validator.Validate(null, new ClaimSubmissionOptions { Mode = "Ftp" });

        Assert.True(result.Failed);
    }

    // ------------------------------------------------------------ provider

    [Fact]
    public void An_unset_provider_resolves_to_sql_server()
    {
        // Defaulting to SQLite would let a deployment write a practice's
        // records to a local file without anyone noticing.
        var configuration = new ConfigurationBuilder().Build();

        Assert.Equal(DatabaseProvider.SqlServer, DependencyInjection.ResolveProvider(configuration));
    }

    [Theory]
    [InlineData("Sqlite", DatabaseProvider.Sqlite)]
    [InlineData("sqlite", DatabaseProvider.Sqlite)]
    [InlineData("SqlServer", DatabaseProvider.SqlServer)]
    public void A_configured_provider_is_honoured(string configured, DatabaseProvider expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = configured })
            .Build();

        Assert.Equal(expected, DependencyInjection.ResolveProvider(configuration));
    }

    [Fact]
    public void An_unrecognised_provider_is_refused()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = "Postgres" })
            .Build();

        Assert.Throws<InvalidOperationException>(() => DependencyInjection.ResolveProvider(configuration));
    }
}
