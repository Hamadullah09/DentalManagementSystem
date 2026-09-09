using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace DentalSurgery.Web.Components.Account;

/// <summary>
/// Sends account email — password resets, address confirmations — through the
/// practice's configured SMTP gateway.
/// <para>
/// This replaces the template's <c>NoOpEmailSender</c>, which discarded every
/// message and returned success. The effect was that "Forgotten your password?"
/// showed a reassuring confirmation page and did nothing at all: a clinician
/// locked out mid-clinic had no route back in, and nobody would discover it
/// until it mattered.
/// </para>
/// <para>
/// Where no gateway is configured this refuses loudly rather than pretending.
/// The reset link is written to the log at Warning so an administrator can
/// still get a locked-out colleague working, and
/// <see cref="AccountEmailUnavailableException"/> is raised so the page can say
/// plainly that no email was sent. Silence is the one behaviour that is not
/// acceptable here.
/// </para>
/// </summary>
public class IdentityEmailSender(
    INotificationSender notifications,
    ILogger<IdentityEmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your password",
            $"""
             <p>A password reset was requested for your account at the practice.</p>
             <p><a href="{resetLink}">Choose a new password</a></p>
             <p>If you did not ask for this, no action is needed and your password is unchanged.</p>
             """,
            $"password reset for {email}", resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your password",
            $"""
             <p>A password reset was requested for your account at the practice.</p>
             <p>Your reset code is <strong>{WebUtility.HtmlEncode(resetCode)}</strong>.</p>
             <p>If you did not ask for this, no action is needed and your password is unchanged.</p>
             """,
            $"password reset code for {email}", resetCode);

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your email address",
            $"""
             <p>Confirm this address to finish setting up your account.</p>
             <p><a href="{confirmationLink}">Confirm my address</a></p>
             """,
            $"address confirmation for {email}", confirmationLink);

    private async Task SendAsync(string email, string subject, string body, string what, string secret)
    {
        if (!notifications.IsChannelConfigured(CommunicationChannel.Email))
        {
            // Logged at Warning, not Error: this is a configuration state the
            // practice may legitimately be in on day one, and an administrator
            // needs the link to unblock the person standing in front of them.
            logger.LogWarning(
                "No email gateway is configured, so the {What} could not be sent. " +
                "An administrator can pass this on directly: {Secret}", what, secret);

            throw new AccountEmailUnavailableException(
                "This practice has no email gateway configured, so the message could not be sent. " +
                "Ask an administrator to set your password directly, or to configure email under " +
                "Administration → Integrations.");
        }

        var result = await notifications.SendAsync(CommunicationChannel.Email, email, subject, body);

        if (result.Delivered)
        {
            logger.LogInformation("Sent the {What}.", what);
            return;
        }

        logger.LogError("Could not send the {What}: {Error}", what, result.Error);

        throw new AccountEmailUnavailableException(
            "The message could not be sent. Ask an administrator to check the email gateway under " +
            "Administration → Integrations.");
    }
}

/// <summary>
/// Raised when account email cannot be delivered. Carries a message written for
/// the person reading the screen, because the alternative — a page that claims
/// an email is on its way when none is — is what this whole class exists to
/// stop.
/// </summary>
public class AccountEmailUnavailableException(string message) : Exception(message);
