using Hangfire;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class MedicationReminderJob(
    IMedicationReminderLogRepository logRepository,
    IUnitOfWork unitOfWork,
    IBackgroundJobClient backgroundJobClient,
    INotificationService notificationService,
    ILogger<MedicationReminderJob> logger)
{
    private const int MaxReminderNumber = 3;
    private const int MinutesBetweenReminders = 10;

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

        if (log.ReminderNumber < MaxReminderNumber)
        {
            var nextLog = new MedicationReminderLog(
                log.MedicineReminderId,
                log.UserHealthProfileId,
                log.ScheduledDate,
                log.ScheduledTime,
                log.ReminderNumber + 1);

            await logRepository.AddAsync(nextLog, CancellationToken.None);

            // Schedule next reminder; ID is available because it's set in constructor
            var nextJobId = backgroundJobClient.Schedule<MedicationReminderJob>(
                job => job.ExecuteAsync(nextLog.Id),
                TimeSpan.FromMinutes(MinutesBetweenReminders));

            log.SetNextJobId(nextJobId);
        }

        logRepository.Update(log);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "Sent medication reminder #{Number} for log {LogId}.",
            log.ReminderNumber, logId);
    }

    private async Task SendNotificationAsync(MedicationReminderLog log)
    {
        var profile = log.UserHealthProfile;

        if (profile?.UserId is null)
        {
            logger.LogWarning(
                "Profile {ProfileId} has no associated user. Skipping notification.",
                log.UserHealthProfileId);
            return;
        }

        var medicineName = log.MedicineReminder?.UserMedicine?.MedicineName ?? "your medication";
        var dosage = log.MedicineReminder?.UserMedicine?.Dosage ?? string.Empty;

        var message = log.ReminderNumber == 1
            ? $"Time to take {medicineName} ({dosage}). Confirm once taken."
            : $"Reminder #{log.ReminderNumber}: Please take {medicineName} ({dosage}) now.";

        var payload = new MedicationReminderNotificationPayload
        {
            ReminderId = log.Id.ToString(),
            MedicineId = log.MedicineReminder?.UserMedicineId.ToString() ?? string.Empty,
            MedicineName = medicineName,
            GenericName = string.Empty,
            Strength = string.Empty,
            Dosage = dosage,
            ReminderTime = log.ScheduledTime.ToString(),
            Status = log.Status.ToString(),
            NotificationText = message
        };

        var targetUserId = profile.UserId.ToString()!;

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
    }
}
