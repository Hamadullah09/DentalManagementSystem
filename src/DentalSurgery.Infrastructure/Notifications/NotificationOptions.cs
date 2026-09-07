namespace DentalSurgery.Infrastructure.Notifications;

/// <summary>
/// Gateway configuration. Every channel is off until it is configured, and the
/// application records messages rather than pretending to send them.
/// </summary>
public class NotificationOptions
{
    public const string SectionName = "Notifications";

    public EmailOptions Email { get; set; } = new();
    public SmsOptions Sms { get; set; } = new();

    /// <summary>
    /// Redirects every outbound message to this address or number instead of the
    /// patient. Essential when running a copy of live data outside production.
    /// </summary>
    public string? RedirectAllTo { get; set; }

    /// <summary>Stops the reminder worker from sending anything.</summary>
    public bool SuppressOutbound { get; set; }

    /// <summary>How often the reminder worker looks for due messages.</summary>
    public int DispatchIntervalMinutes { get; set; } = 5;

    /// <summary>Messages older than this are abandoned rather than sent late.</summary>
    public int AbandonAfterHours { get; set; } = 12;

    public int MaxAttempts { get; set; } = 3;
}

public class EmailOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>STARTTLS on the submission port. Set false only for port 465 implicit TLS.</summary>
    public bool UseStartTls { get; set; } = true;
    public bool UseSsl { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Dental Surgery";
    public string? ReplyTo { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsUsable => Enabled &&
                            !string.IsNullOrWhiteSpace(Host) &&
                            !string.IsNullOrWhiteSpace(FromAddress);
}

public class SmsOptions
{
    public bool Enabled { get; set; }

    /// <summary>
    /// The gateway shape. "TwilioCompatible" posts form fields (To/From/Body)
    /// with basic auth, which Twilio and several UK resellers accept. "Json"
    /// posts a JSON body built from the field names below.
    /// </summary>
    public string Provider { get; set; } = "TwilioCompatible";

    public string Endpoint { get; set; } = string.Empty;
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string? BearerToken { get; set; }
    public string FromNumber { get; set; } = string.Empty;

    public string ToField { get; set; } = "To";
    public string FromField { get; set; } = "From";
    public string BodyField { get; set; } = "Body";

    /// <summary>Where the provider's message identifier appears in the response.</summary>
    public string MessageIdField { get; set; } = "sid";

    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Longer messages are split by the carrier and billed per segment.</summary>
    public int MaxLength { get; set; } = 480;

    public bool IsUsable => Enabled &&
                            !string.IsNullOrWhiteSpace(Endpoint) &&
                            !string.IsNullOrWhiteSpace(FromNumber);
}

/// <summary>Where generated claim files go and how they are transmitted.</summary>
public class ClaimSubmissionOptions
{
    public const string SectionName = "ClaimSubmission";

    /// <summary>"Manual", "FileDrop", "Http" or "None".</summary>
    public string Mode { get; set; } = "Manual";

    /// <summary>Folder the clearing house collects from.</summary>
    public string OutboundFolder { get; set; } = "App_Data/claims/outbound";
    public string ArchiveFolder { get; set; } = "App_Data/claims/archive";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 60;

    // ---- interchange identifiers, issued by the clearing house ----
    // These are deliberately empty. They identify the practice to a payer, and a
    // plausible-looking default would be submitted on real claims by any
    // deployment that had not configured them yet. ClaimSubmissionOptionsValidator
    // refuses to start a transmitting instance until they are set.
    public string SubmitterId { get; set; } = string.Empty;
    public string SubmitterName { get; set; } = string.Empty;
    public string SubmitterContact { get; set; } = string.Empty;
    public string SubmitterPhone { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string InterchangeSenderId { get; set; } = string.Empty;
    public string InterchangeReceiverId { get; set; } = string.Empty;

    /// <summary>Billing provider identifier. An NPI in the US, a performer number elsewhere.</summary>
    public string BillingProviderId { get; set; } = string.Empty;
    public string BillingProviderTaxId { get; set; } = string.Empty;

    /// <summary>"T" for test, "P" for production. Never send live claims as test.</summary>
    public string UsageIndicator { get; set; } = "T";
}
