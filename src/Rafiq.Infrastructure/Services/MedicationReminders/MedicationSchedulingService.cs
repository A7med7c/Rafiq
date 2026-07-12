using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

/// <summary>
/// The one place that decides whether a reminder is due today, materialises its log,
/// and hands the delivery job to Hangfire. Both the daily scheduler and the create-reminder
/// flow go through here, so a reminder created after the daily sweep still fires today.
/// </summary>
public sealed class MedicationSchedulingService(
    IMedicationReminderLogRepository logRepository,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork,
    IBackgroundJobClient backgroundJobClient,
    IDateTimeProvider dateTimeProvider,
    IOptions<MedicationReminderOptions> options,
    ILogger<MedicationSchedulingService> logger)
    : IMedicationSchedulingService
{
    private readonly TimeSpan _lateGrace = TimeSpan.FromMinutes(options.Value.LateGraceMinutes);

    public async Task<bool> ScheduleTodayIfApplicableAsync(
        MedicineReminder reminder,
        CancellationToken cancellationToken = default)
    {
        var today = dateTimeProvider.Today;

        if (!IsApplicableForDate(reminder, today))
            return false;

        if (await logRepository.ExistsForDateAsync(reminder.Id, today, cancellationToken))
        {
            logger.LogDebug(
                "Reminder {ReminderId} is already scheduled for {Date}.", reminder.Id, today);
            return false;
        }

        var profileId = await ResolveHealthProfileIdAsync(reminder, cancellationToken);
        if (profileId is null)
        {
            logger.LogWarning(
                "Cannot schedule reminder {ReminderId}: UserMedicine {UserMedicineId} was not found.",
                reminder.Id, reminder.UserMedicineId);
            return false;
        }

        var scheduledUtc = dateTimeProvider.ToUtc(today, reminder.ReminderTime);
        var delay = scheduledUtc - dateTimeProvider.UtcNow;

        // Every dose planned for today gets a log, even one whose time has already gone.
        // Today's Schedule, adherence and history must show the whole day, not just its future.
        var log = new MedicationReminderLog(
            reminder.Id,
            profileId.Value,
            today,
            reminder.ReminderTime,
            reminderNumber: 1);

        // Past the grace window there is no point notifying — the dose is simply recorded
        // as missed. Inside it (or still ahead), the reminder is delivered as normal.
        var notify = delay >= -_lateGrace;

        if (!notify)
            log.MarkAsOverdue();

        await logRepository.AddAsync(log, cancellationToken);

        // The log must be committed before any job is queued: with a zero delay Hangfire can
        // run MedicationReminderJob immediately, and it looks the log up by id.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!notify)
        {
            logger.LogInformation(
                "Recorded reminder {ReminderId} (log {LogId}) as Overdue: it was due at {ScheduledUtc:o}, beyond the {Grace} grace window. No notification sent.",
                reminder.Id, log.Id, scheduledUtc, _lateGrace);

            return true;
        }

        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        backgroundJobClient.Schedule<MedicationReminderJob>(
            job => job.ExecuteAsync(log.Id),
            delay);

        reminder.RecordTrigger();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Scheduled reminder {ReminderId} (log {LogId}) for {ScheduledUtc:o} (in {Delay}).",
            reminder.Id, log.Id, scheduledUtc, delay);

        return true;
    }

    /// <summary>
    /// <paramref name="date"/> is a date in the reminder's timezone, never a UTC date.
    /// </summary>
    private static bool IsApplicableForDate(MedicineReminder reminder, DateOnly date)
    {
        if (!reminder.IsEnabled || reminder.IsDeleted)
            return false;

        if (date < reminder.StartDate || date > reminder.EndDate)
            return false;

        return reminder.RepeatType switch
        {
            RepeatType.Once => reminder.StartDate == date,
            RepeatType.Daily => true,
            RepeatType.Weekly => (date.DayNumber - reminder.StartDate.DayNumber) % 7 == 0,
            RepeatType.Monthly => date.Day == reminder.StartDate.Day,
            _ => false
        };
    }

    /// <summary>
    /// A freshly created reminder has no UserMedicine navigation loaded, so fall back to a lookup.
    /// </summary>
    private async Task<Guid?> ResolveHealthProfileIdAsync(
        MedicineReminder reminder,
        CancellationToken cancellationToken)
    {
        if (reminder.UserMedicine is not null)
            return reminder.UserMedicine.UserHealthProfileId;

        var userMedicine = await userMedicineRepository.GetByIdAsync(
            reminder.UserMedicineId, cancellationToken);

        return userMedicine?.UserHealthProfileId;
    }
}
