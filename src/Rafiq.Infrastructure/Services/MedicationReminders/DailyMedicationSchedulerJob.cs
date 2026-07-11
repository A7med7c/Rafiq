using Hangfire;
using Microsoft.Extensions.Logging;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class DailyMedicationSchedulerJob(
    IMedicineReminderRepository medicineReminderRepository,
    IMedicationReminderLogRepository logRepository,
    IUnitOfWork unitOfWork,
    IBackgroundJobClient backgroundJobClient,
    ILogger<DailyMedicationSchedulerJob> logger)
{
    public async Task ScheduleAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reminders = await medicineReminderRepository.GetActiveForDateAsync(today, CancellationToken.None);

        var applicable = reminders.Where(r => IsApplicableForDate(r, today)).ToList();

        if (applicable.Count == 0)
        {
            logger.LogInformation("No active medication reminders to schedule for {Date}.", today);
            return;
        }

        var pendingLogs = new List<(MedicationReminderLog Log, MedicineReminder Reminder)>();

        foreach (var reminder in applicable)
        {
            var alreadyExists = await logRepository.ExistsForDateAsync(
                reminder.Id, today, CancellationToken.None);

            if (alreadyExists)
                continue;

            var log = new MedicationReminderLog(
                reminder.Id,
                reminder.UserMedicine.UserHealthProfileId,
                today,
                reminder.ReminderTime,
                reminderNumber: 1);

            await logRepository.AddAsync(log, CancellationToken.None);
            pendingLogs.Add((log, reminder));
        }

        if (pendingLogs.Count == 0)
        {
            logger.LogInformation("All medication reminders for {Date} are already scheduled.", today);
            return;
        }

        // Persist logs first so the IDs are tracked before scheduling jobs
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        foreach (var (log, reminder) in pendingLogs)
        {
            var scheduledUtc = today.ToDateTime(
                TimeOnly.FromTimeSpan(reminder.ReminderTime),
                DateTimeKind.Utc);

            var delay = scheduledUtc - DateTime.UtcNow;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            backgroundJobClient.Schedule<MedicationReminderJob>(
                job => job.ExecuteAsync(log.Id),
                delay);

            reminder.RecordTrigger();
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation(
            "Scheduled {Count} first-reminder job(s) for {Date}.",
            pendingLogs.Count, today);
    }

    private static bool IsApplicableForDate(MedicineReminder reminder, DateOnly date)
    {
        return reminder.RepeatType switch
        {
            RepeatType.Once => reminder.StartDate == date,
            RepeatType.Daily => true,
            RepeatType.Weekly => (date.DayNumber - reminder.StartDate.DayNumber) % 7 == 0,
            RepeatType.Monthly => date.Day == reminder.StartDate.Day,
            _ => false
        };
    }
}
