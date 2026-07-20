using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;
using Rafiq.Infrastructure.Services.Notifications;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class MedicationReminderJob(
    IMedicationReminderLogRepository logRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IHealthProfileAccessRepository healthProfileAccessRepository,
    IWhatsAppService whatsAppService,
    UserManager<ApplicationUser> userManager,
    IOptions<WhatsAppSettings> whatsAppOptions,
    ILogger<MedicationReminderJob> logger)
{
    // Managed Profiles have no owning user, so reminders route to the members responsible for
    // managing them. Viewer-only members are deliberately excluded from medication reminders.
    private static readonly AccessRole[] ManagementRoles = [AccessRole.Owner, AccessRole.Manager];

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid logId)
    {
        logger.LogInformation("MedicationReminderJob STARTED for log {LogId}.", logId);

        var log = await logRepository.GetByIdWithDetailsAsync(logId, CancellationToken.None);

        if (log is null)
        {
            logger.LogWarning("MedicationReminderLog {LogId} not found. Skipping.", logId);
            return;
        }

        logger.LogInformation(
            "ReminderLog loaded: status={Status}, reminderNumber={Number}, profileId={ProfileId}, userId={UserId}",
            log.Status, log.ReminderNumber, log.UserHealthProfileId, log.UserHealthProfile?.UserId);

        var reminder = log.MedicineReminder;

        if (reminder is null)
        {
            logger.LogWarning(
                "MedicineReminder for log {LogId} not found. Skipping.", logId);
            return;
        }

        if (reminder.IsDeleted)
        {
            logger.LogInformation(
                "MedicineReminder {ReminderId} has been deleted. Skipping log {LogId}.",
                reminder.Id, logId);
            return;
        }

        if (!reminder.IsEnabled)
        {
            logger.LogInformation(
                "MedicineReminder {ReminderId} is disabled. Skipping log {LogId}.",
                reminder.Id, logId);
            return;
        }

        if (log.Status == MedicationReminderStatus.Cancelled)
        {
            logger.LogInformation(
                "MedicationReminderLog {LogId} is cancelled. Skipping.", logId);
            return;
        }

        if (log.IsCompleted)
        {
            logger.LogInformation(
                "MedicationReminderLog {LogId} is already {Status}. Skipping.",
                logId, log.Status);
            return;
        }

        logger.LogInformation("Before SendNotificationAsync for log {LogId}.", logId);
        await SendNotificationAsync(log);
        logger.LogInformation("After SendNotificationAsync for log {LogId}.", logId);

        log.MarkAsSent();

        // All three stages are scheduled up front by MedicationSchedulingService — this job
        // only delivers the stage it was given, it never creates or schedules another one.
        logRepository.Update(log);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "Sent medication reminder #{Number} for log {LogId}.",
            log.ReminderNumber, logId);
    }

    /// <summary>Formats a <see cref="TimeSpan"/> as "hh:mm AM/PM".</summary>
    private static string FormatTimeSpan(TimeSpan time)
    {
        var totalMinutes = (int)time.TotalMinutes;
        var h = (totalMinutes / 60) % 24;
        var m = totalMinutes % 60;
        var period = h >= 12 ? "PM" : "AM";
        var hour12 = h % 12 == 0 ? 12 : h % 12;
        return $"{hour12:D2}:{m:D2} {period}";
    }

    private async Task SendNotificationAsync(MedicationReminderLog log)
    {
        var profile = log.UserHealthProfile;

        if (profile is null)
        {
            logger.LogWarning(
                "ReminderLog {LogId} has no health profile loaded. Skipping notification.",
                log.Id);
            return;
        }

        // Case 1: Self Profile — deliver to the owning user. Case 2: Managed Profile (UserId == null)
        // — deliver to every active Owner/Manager. Distinct ids from the repository guarantee each
        // responsible user is notified exactly once, even with duplicate access records.
        var recipientUserIds = profile.UserId is not null
            ? [profile.UserId.Value]
            : await healthProfileAccessRepository.GetActiveGranteeUserIdsByRolesAsync(
                profile.Id, ManagementRoles, CancellationToken.None);

        if (recipientUserIds.Count == 0)
        {
            logger.LogInformation(
                "Profile {ProfileId} has no active Owner/Manager recipient. Skipping medication reminder.",
                profile.Id);
            return;
        }

        var medicineName = log.MedicineReminder?.UserMedicine?.MedicineName ?? "your medication";
        var dosage = log.MedicineReminder?.UserMedicine?.Dosage ?? string.Empty;

        var message = log.ReminderNumber switch
        {
            1 => $"{medicineName} ({dosage}) is due soon. Get ready to take it.",
            2 => $"Time to take {medicineName} ({dosage}). Confirm once taken.",
            _ => $"{medicineName} ({dosage}) is overdue. Please confirm if taken.",
        };

        // Use the MedicineReminder's configured time (not the attempt's offset time) so the
        // modal always shows the dose time the user set, not the escalation fire time.
        var configuredTime = log.MedicineReminder?.ReminderTime ?? log.ScheduledTime;
        var reminderTimeFormatted = FormatTimeSpan(configuredTime);

        var payload = new MedicationReminderNotificationPayload
        {
            ReminderId = log.Id.ToString(),
            MedicineId = log.MedicineReminder?.UserMedicineId.ToString() ?? string.Empty,
            MedicineName = medicineName,
            GenericName = string.Empty,
            // Strength and Dosage carry the same value (the user's entered dosage/strength).
            // The modal displays Strength; setting both avoids a "Not specified" fallback.
            Strength = dosage,
            Dosage = dosage,
            ReminderTime = reminderTimeFormatted,
            Status = log.Status.ToString(),
            NotificationText = message
        };

        foreach (var recipientUserId in recipientUserIds)
        {
            var targetUserId = recipientUserId.ToString();

            logger.LogInformation(
                "Before notificationService.SendMedicationReminderAsync: targetUserId='{UserId}', logId={LogId}",
                targetUserId, log.Id);

            await notificationService.SendMedicationReminderAsync(
                targetUserId,
                payload,
                CancellationToken.None);

            logger.LogInformation(
                "After notificationService.SendMedicationReminderAsync: userId={UserId}, logId={LogId}",
                targetUserId, log.Id);

            // Stage 2 is the primary at-dose-time reminder — also deliver it via WhatsApp so
            // recipients receive it even when the app is backgrounded or offline.
            if (log.ReminderNumber == 2)
            {
                await SendWhatsAppMedicineReminderAsync(log, recipientUserId, medicineName);
            }
        }
    }

    private async Task SendWhatsAppMedicineReminderAsync(
        MedicationReminderLog log,
        Guid userId,
        string medicineName)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.PhoneNumber is null)
        {
            logger.LogWarning(
                "No phone number found for user {UserId}. Skipping WhatsApp reminder for log {LogId}.",
                userId, log.Id);
            return;
        }

        var patientName = $"{log.UserHealthProfile.FirstName} {log.UserHealthProfile.LastName}";

        var templateName = whatsAppOptions.Value.PrimaryReminderTemplate;

        logger.LogInformation(
            "Sending WhatsApp template '{Template}' to '{Phone}' for log {LogId}.",
            templateName, user.PhoneNumber, log.Id);

        try
        {
            await whatsAppService.SendTemplateAsync(
                user.PhoneNumber,
                templateName,
                [patientName, medicineName],
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // WhatsApp failure must not abort the job — the SignalR notification already fired.
            logger.LogError(ex,
                "Failed to send WhatsApp reminder for log {LogId} to user {UserId}.",
                log.Id, userId);
        }
    }
}
