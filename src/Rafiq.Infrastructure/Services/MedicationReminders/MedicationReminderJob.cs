using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class MedicationReminderJob(
    IMedicationReminderLogRepository logRepository,
    UserManager<ApplicationUser> userManager,
    IUnitOfWork unitOfWork,
    IBackgroundJobClient backgroundJobClient,
    ILogger<MedicationReminderJob> logger)
{
    private const int MaxReminderNumber = 3;
    private const int MinutesBetweenReminders = 10;

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid logId)
    {
        var log = await logRepository.GetByIdWithDetailsAsync(logId, CancellationToken.None);

        if (log is null)
        {
            logger.LogWarning("MedicationReminderLog {LogId} not found. Skipping.", logId);
            return;
        }

        if (log.IsCompleted)
        {
            logger.LogInformation(
                "MedicationReminderLog {LogId} is already {Status}. Skipping.",
                logId, log.Status);
            return;
        }

        await SendNotificationAsync(log);

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

        var user = await userManager.FindByIdAsync(profile.UserId.ToString()!);

        if (user is null || string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            logger.LogWarning(
                "User {UserId} has no phone number. Skipping notification.",
                profile.UserId);
            return;
        }

        var medicineName = log.MedicineReminder?.UserMedicine?.MedicineName ?? "your medication";
        var dosage = log.MedicineReminder?.UserMedicine?.Dosage ?? string.Empty;

        var message = log.ReminderNumber == 1
            ? $"Time to take {medicineName} ({dosage}). Confirm once taken."
            : $"Reminder #{log.ReminderNumber}: Please take {medicineName} ({dosage}) now.";

        // The notification delivery (SMS/push) is handled by the notification infrastructure.
        // This is the integration point: SendMedicationReminder(userId, logId, message).
        logger.LogInformation(
            "SendMedicationReminder → userId={UserId}, logId={LogId}, phone={Phone}, message={Message}",
            profile.UserId, log.Id, user.PhoneNumber, message);
    }
}
