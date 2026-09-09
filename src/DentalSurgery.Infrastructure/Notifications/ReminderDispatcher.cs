using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalSurgery.Infrastructure.Notifications;

/// <summary>Merge tokens available to message templates.</summary>
public static class MessageTokens
{
    public static string Apply(string template, IReadOnlyDictionary<string, string?> values)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var result = template;
        foreach (var (token, value) in values)
            result = result.Replace("{" + token + "}", value ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        return result;
    }

    public static Dictionary<string, string?> For(
        Patient? patient, Appointment? appointment, Practice? practice, RecallSchedule? recall = null)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PatientFirstName"] = patient?.Name.PreferredName ?? patient?.Name.FirstName ?? "there",
            ["PatientName"] = patient?.Name.Display,
            ["PatientNumber"] = patient?.PatientNumber,
            ["AppointmentDate"] = appointment?.StartUtc.ToString("dddd d MMMM"),
            ["AppointmentTime"] = appointment?.StartUtc.ToString("HH:mm"),
            ["ProviderName"] = appointment?.Provider?.DisplayName,
            ["RecallType"] = recall is null ? null : PdfSafe(recall.DisplayName),
            ["DueDate"] = recall?.DueDate.ToString("d MMMM yyyy"),
            ["PracticeName"] = practice?.Name ?? "your dental practice",
            ["PracticePhone"] = practice?.Contact.HomePhone ?? practice?.Contact.MobilePhone,
            ["PracticeAddress"] = practice?.Address.ToSingleLine()
        };
    }

    private static string PdfSafe(string value) => value.Trim();
}

/// <summary>
/// Sends appointment reminders as they fall due. Runs continuously; each pass
/// takes only the messages whose send time has arrived, marks the outcome, and
/// writes a communications-log entry so the practice can see what went out.
/// </summary>
public class ReminderDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationOptions> options,
    IDateTimeProvider clock,
    ILogger<ReminderDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.DispatchIntervalMinutes));

        // Let the application finish starting and the database finish seeding.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        logger.LogInformation("Reminder dispatcher started; checking every {Minutes} minutes.", interval.TotalMinutes);

        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                var sent = await DispatchDueAsync(stoppingToken);
                if (sent > 0) logger.LogInformation("Dispatched {Count} reminder(s).", sent);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // A bad pass must not kill the worker for the rest of the process lifetime.
                logger.LogError(ex, "The reminder pass failed; it will be retried.");
            }
        }
        while (await SafeWait(timer, stoppingToken));

        logger.LogInformation("Reminder dispatcher stopped.");
    }

    private static async Task<bool> SafeWait(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    /// <summary>
    /// Sends every reminder whose scheduled time has passed, for every active
    /// tenant. Returns how many were attempted across all of them.
    /// <para>
    /// A background sweep has no signed-in user and therefore no tenant, and the
    /// query filters would match nothing at all — the worker would run happily
    /// and send no reminders. It therefore enumerates tenants from a platform
    /// scope and enters each in turn, so every query inside the sweep is still
    /// confined to exactly one practice.
    /// </para>
    /// </summary>
    public async Task<int> DispatchDueAsync(CancellationToken ct)
    {
        // A hosted service is a singleton, so the per-request services this pass
        // needs come from a scope of its own.
        using var scope = scopeFactory.CreateScope();
        var tenants = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
        var scopes = scope.ServiceProvider.GetRequiredService<ITenantScopeFactory>();

        var attempted = 0;

        foreach (var tenant in await tenants.ActiveTenantsAsync(ct))
        {
            if (ct.IsCancellationRequested) break;

            using (scopes.EnterTenant(tenant.Id))
            {
                attempted += await DispatchForTenantAsync(scope.ServiceProvider, ct);
            }
        }

        return attempted;
    }

    /// <summary>One tenant's due reminders. Always called inside that tenant's scope.</summary>
    private async Task<int> DispatchForTenantAsync(IServiceProvider services, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var abandonBefore = now.AddHours(-Math.Max(1, options.Value.AbandonAfterHours));

        var dbFactory = services.GetRequiredService<IDbContextFactory<DentalDbContext>>();
        var notifications = services.GetRequiredService<INotificationSender>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var due = await db.AppointmentReminders
            .Include(r => r.Appointment).ThenInclude(a => a!.Patient)
            .Include(r => r.Appointment).ThenInclude(a => a!.Provider)
            .Where(r => r.Status == ReminderStatus.Pending && r.ScheduledForUtc <= now)
            .OrderBy(r => r.ScheduledForUtc)
            .Take(100)
            .ToListAsync(ct);

        if (due.Count == 0) return 0;

        var practice = await db.Practices.AsNoTracking().FirstOrDefaultAsync(ct);
        var templates = await db.MessageTemplates.AsNoTracking()
            .Where(t => t.IsActive && t.Category == "Reminder")
            .ToListAsync(ct);

        var attempted = 0;

        foreach (var reminder in due)
        {
            var appointment = reminder.Appointment;

            // Nothing to remind about if the visit was cancelled or already happened.
            if (appointment is null || !appointment.IsActiveBooking || appointment.StartUtc <= now)
            {
                reminder.Status = ReminderStatus.CancelRequested;
                reminder.FailureReason = "The appointment is no longer active.";
                continue;
            }

            if (reminder.ScheduledForUtc < abandonBefore)
            {
                reminder.Status = ReminderStatus.Failed;
                reminder.FailureReason = "Not sent in time; abandoned rather than sent late.";
                logger.LogWarning("Abandoned stale reminder for appointment {Number}.", appointment.AppointmentNumber);
                continue;
            }

            var patient = appointment.Patient;
            if (patient is null) continue;

            // Respect the patient's contact preferences.
            if (!ChannelAllowed(patient, reminder.Channel))
            {
                reminder.Status = ReminderStatus.CancelRequested;
                reminder.FailureReason = $"The patient has opted out of {reminder.Channel}.";
                continue;
            }

            var recipient = reminder.Channel switch
            {
                CommunicationChannel.Email => patient.Contact.Email,
                CommunicationChannel.Sms => patient.Contact.MobilePhone,
                _ => patient.Contact.BestPhone
            };

            if (string.IsNullOrWhiteSpace(recipient))
            {
                reminder.Status = ReminderStatus.Failed;
                reminder.FailureReason = $"No {reminder.Channel} address is recorded for the patient.";
                continue;
            }

            var template = templates.FirstOrDefault(t => t.Channel == reminder.Channel)
                           ?? templates.FirstOrDefault();

            var tokens = MessageTokens.For(patient, appointment, practice);
            var subject = MessageTokens.Apply(template?.Subject ?? "Your appointment on {AppointmentDate}", tokens);
            var body = MessageTokens.Apply(
                template?.Body ?? "Hi {PatientFirstName}, this is a reminder of your appointment on " +
                                  "{AppointmentDate} at {AppointmentTime}. {PracticeName}", tokens);

            reminder.AttemptCount++;
            reminder.Recipient = recipient;
            reminder.MessageBody = body;
            attempted++;

            var result = await notifications.SendAsync(reminder.Channel, recipient, subject, body, ct);

            if (result.Delivered)
            {
                reminder.Status = ReminderStatus.Sent;
                reminder.SentAtUtc = now;
                reminder.FailureReason = null;
            }
            else if (result.WasRecordedOnly)
            {
                // No gateway configured: record it so the front desk can follow up by phone.
                reminder.Status = ReminderStatus.Sent;
                reminder.SentAtUtc = now;
                reminder.FailureReason = "Recorded only; no gateway is configured for this channel.";
            }
            else if (reminder.AttemptCount >= options.Value.MaxAttempts)
            {
                reminder.Status = ReminderStatus.Failed;
                reminder.FailureReason = result.Error;
            }
            else
            {
                // Leave it pending so the next pass retries.
                reminder.ScheduledForUtc = now.AddMinutes(15);
                reminder.FailureReason = result.Error;
            }

            db.CommunicationLogs.Add(new CommunicationLog
            {
                PatientId = patient.Id,
                OccurredAtUtc = now,
                Channel = reminder.Channel,
                Direction = CommunicationDirection.Outbound,
                Status = result.Delivered ? CommunicationStatus.Sent
                    : result.WasRecordedOnly ? CommunicationStatus.Queued
                    : CommunicationStatus.Failed,
                Subject = subject,
                Body = body,
                Recipient = recipient,
                Sender = result.Provider,
                AppointmentId = appointment.Id,
                MessageTemplateId = template?.Id,
                FailureReason = result.Error
            });
        }

        await db.SaveChangesAsync(ct);
        return attempted;
    }

    private static bool ChannelAllowed(Patient patient, CommunicationChannel channel) => channel switch
    {
        CommunicationChannel.Email => patient.AllowEmail,
        CommunicationChannel.Sms => patient.AllowSms,
        CommunicationChannel.Phone => patient.AllowPhoneCall,
        CommunicationChannel.Letter => patient.AllowPost,
        _ => true
    };
}
