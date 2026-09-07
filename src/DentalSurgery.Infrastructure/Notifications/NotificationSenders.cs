using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DentalSurgery.Infrastructure.Notifications;

/// <summary>Sends email over SMTP submission.</summary>
public class SmtpEmailSender(
    IOptions<NotificationOptions> options,
    ILogger<SmtpEmailSender> logger)
{
    private EmailOptions Config => options.Value.Email;

    public bool IsConfigured => Config.IsUsable;

    public async Task<NotificationResult> SendAsync(
        string recipient, string? subject, string body, CancellationToken ct)
    {
        if (!IsConfigured) return NotificationResult.Recorded();

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Config.FromName, Config.FromAddress));
            message.To.Add(MailboxAddress.Parse(recipient));

            if (!string.IsNullOrWhiteSpace(Config.ReplyTo))
                message.ReplyTo.Add(MailboxAddress.Parse(Config.ReplyTo));

            message.Subject = string.IsNullOrWhiteSpace(subject) ? "A message from your dental practice" : subject;
            message.Body = new TextPart(TextFormat.Plain) { Text = body };

            using var client = new SmtpClient { Timeout = Config.TimeoutSeconds * 1000 };

            var security = Config.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : Config.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            await client.ConnectAsync(Config.Host, Config.Port, security, ct);

            if (!string.IsNullOrWhiteSpace(Config.Username))
                await client.AuthenticateAsync(Config.Username, Config.Password ?? string.Empty, ct);

            var response = await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email accepted by {Host} for {Recipient}.", Config.Host, Mask(recipient));
            return NotificationResult.Sent("smtp", string.IsNullOrWhiteSpace(response) ? message.MessageId : response.Trim());
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or
                                       AuthenticationException or SslHandshakeException or
                                       IOException or OperationCanceledException or FormatException)
        {
            logger.LogError(ex, "Email to {Recipient} failed.", Mask(recipient));
            return NotificationResult.Failed("smtp", ex.Message);
        }
    }

    private static string Mask(string address)
    {
        if (!address.Contains('@')) return "***";
        var parts = address.Split('@');
        return $"{parts[0][..Math.Min(2, parts[0].Length)]}***@{parts[^1]}";
    }
}

/// <summary>
/// Posts to an HTTP SMS gateway. Two shapes are supported: form-encoded with
/// basic auth, which Twilio and several resellers accept, and a JSON body.
/// </summary>
public class HttpSmsSender(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationOptions> options,
    ILogger<HttpSmsSender> logger)
{
    private SmsOptions Config => options.Value.Sms;

    public bool IsConfigured => Config.IsUsable;

    public async Task<NotificationResult> SendAsync(string recipient, string body, CancellationToken ct)
    {
        if (!IsConfigured) return NotificationResult.Recorded();

        // Truncate rather than let the carrier bill for unexpected segments.
        var text = body.Length <= Config.MaxLength ? body : body[..(Config.MaxLength - 1)] + "…";

        try
        {
            var client = httpClientFactory.CreateClient("sms");
            client.Timeout = TimeSpan.FromSeconds(Config.TimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, Config.Endpoint);

            if (!string.IsNullOrWhiteSpace(Config.BearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Config.BearerToken);
            }
            else if (!string.IsNullOrWhiteSpace(Config.AccountSid))
            {
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{Config.AccountSid}:{Config.AuthToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }

            if (Config.Provider.Equals("Json", StringComparison.OrdinalIgnoreCase))
            {
                var payload = new Dictionary<string, string>
                {
                    [Config.ToField] = recipient,
                    [Config.FromField] = Config.FromNumber,
                    [Config.BodyField] = text
                };
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            }
            else
            {
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [Config.ToField] = recipient,
                    [Config.FromField] = Config.FromNumber,
                    [Config.BodyField] = text
                });
            }

            using var response = await client.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("SMS gateway returned {Status} for {Recipient}: {Body}",
                    (int)response.StatusCode, Mask(recipient), Truncate(responseBody));
                return NotificationResult.Failed("sms", $"Gateway returned {(int)response.StatusCode}: {Truncate(responseBody)}");
            }

            logger.LogInformation("SMS accepted by the gateway for {Recipient}.", Mask(recipient));
            return NotificationResult.Sent("sms", ExtractMessageId(responseBody));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogError(ex, "SMS to {Recipient} failed.", Mask(recipient));
            return NotificationResult.Failed("sms", ex.Message);
        }
    }

    /// <summary>Pulls the provider's message id out of the response, when present.</summary>
    private string? ExtractMessageId(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(Config.MessageIdField, out var element))
            {
                return element.ToString();
            }
        }
        catch (JsonException)
        {
            // Not JSON; the raw body is the best identifier available.
        }

        return Truncate(responseBody, 64);
    }

    private static string Truncate(string value, int length = 200) =>
        value.Length <= length ? value : value[..length];

    private static string Mask(string number) =>
        number.Length <= 4 ? "***" : $"***{number[^4..]}";
}

/// <summary>
/// Routes a message to whichever gateway serves its channel. When a channel has
/// no gateway the message is recorded rather than silently dropped, which is
/// what the practice sees in the communications log.
/// </summary>
public class CompositeNotificationSender(
    SmtpEmailSender email,
    HttpSmsSender sms,
    IOptions<NotificationOptions> options,
    ILogger<CompositeNotificationSender> logger) : INotificationSender
{
    public bool IsChannelConfigured(CommunicationChannel channel) => channel switch
    {
        CommunicationChannel.Email => email.IsConfigured,
        CommunicationChannel.Sms => sms.IsConfigured,
        _ => false
    };

    public async Task<NotificationResult> SendAsync(
        CommunicationChannel channel, string recipient, string? subject, string body,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            return NotificationResult.Failed("none", "No recipient address or number is recorded.");

        if (options.Value.SuppressOutbound)
        {
            logger.LogInformation("Outbound messaging is suppressed; {Channel} message recorded only.", channel);
            return NotificationResult.Recorded();
        }

        // A non-production copy must never message real patients.
        var target = recipient;
        if (!string.IsNullOrWhiteSpace(options.Value.RedirectAllTo))
        {
            logger.LogWarning("Redirecting {Channel} message intended for {Original} to {Target}.",
                channel, recipient, options.Value.RedirectAllTo);
            target = options.Value.RedirectAllTo!;
            body = $"[Intended for {recipient}]\n\n{body}";
        }

        return channel switch
        {
            CommunicationChannel.Email => await email.SendAsync(target, subject, body, ct),
            CommunicationChannel.Sms => await sms.SendAsync(target, body, ct),
            _ => NotificationResult.Recorded()
        };
    }
}
