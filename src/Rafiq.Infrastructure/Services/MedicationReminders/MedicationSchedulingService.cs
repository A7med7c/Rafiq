using Hangfire;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

    // The three fixed stages every reminder occurrence must produce, relative to ReminderTime.
    private const int StageOffsetMinutes = 10;

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

        // The "at time" instant anchors all three stages; ±10 minutes is applied to the
        // absolute UTC instant (not the wall-clock TimeSpan) so a reminder near midnight
        // doesn't produce an out-of-range time-of-day.
        var anchorUtc = dateTimeProvider.ToUtc(today, reminder.ReminderTime);

        var stageDefinitions = new (int ReminderNumber, TimeSpan Offset)[]
        {
            (1, TimeSpan.FromMinutes(-StageOffsetMinutes)),
            (2, TimeSpan.Zero),
            (3, TimeSpan.FromMinutes(StageOffsetMinutes)),
        };

        // Every dose planned for today gets all three stage logs, even ones whose time has
        // already gone. Today's Schedule, adherence and history must show the whole day, not
        // just its future.
        var stages = new List<(MedicationReminderLog Log, DateTime ScheduledUtc, bool Notify)>();

        foreach (var (reminderNumber, offset) in stageDefinitions)
        {
            var stageUtc = anchorUtc + offset;
            var delay = stageUtc - dateTimeProvider.UtcNow;

            // Past the grace window there is no point notifying — the dose is simply recorded
            // as missed. Inside it (or still ahead), the reminder is delivered as normal.
            var notify = delay >= -_lateGrace;

            var log = new MedicationReminderLog(
                reminder.Id,
                profileId.Value,
                today,
                ClampToDay(reminder.ReminderTime + offset),
                reminderNumber);

            if (!notify)
                log.MarkAsOverdue();

            await logRepository.AddAsync(log, cancellationToken);
            stages.Add((log, stageUtc, notify));
        }

        // All three logs must be committed before any job is queued: with a zero delay
        // Hangfire can run MedicationReminderJob immediately, and it looks the log up by id.
        //
        // ExistsForDateAsync above is only an optimistic pre-check — it can't stop two
        // concurrent callers (e.g. the daily sweep racing a manual create) from both passing
        // it and both reaching this insert. The unique filtered index on
        // (MedicineReminderId, ScheduledDate, ReminderNumber) is the actual guarantee: the
        // loser's insert fails here, and that failure is the signal that someone else already
        // scheduled this occurrence — not a real error, so no jobs get queued for it.
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogInformation(
                "Reminder {ReminderId} was scheduled for {Date} by a concurrent request; skipping duplicate.",
                reminder.Id, today);
            return false;
        }

        foreach (var (log, scheduledUtc, notify) in stages)
        {
            if (!notify)
            {
                logger.LogInformation(
                    "Recorded reminder {ReminderId} stage #{Number} (log {LogId}) as Overdue: it was due at {ScheduledUtc:o}, beyond the {Grace} grace window. No notification scheduled.",
                    reminder.Id, log.ReminderNumber, log.Id, scheduledUtc, _lateGrace);
                continue;
            }

            var delay = scheduledUtc - dateTimeProvider.UtcNow;
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            var jobId = backgroundJobClient.Schedule<MedicationReminderJob>(
                job => job.ExecuteAsync(log.Id),
                delay);

            log.SetNextJobId(jobId);
            logRepository.Update(log);

            logger.LogInformation(
                "Scheduled reminder {ReminderId} stage #{Number} (log {LogId}) for {ScheduledUtc:o} (in {Delay}).",
                reminder.Id, log.ReminderNumber, log.Id, scheduledUtc, delay);
        }

        reminder.RecordTrigger();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// SQL Server error 2601 (unique index) / 2627 (unique constraint) — the two codes a
    /// violation of the unique filtered index on MedicationReminderLogs can surface as.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    /// <summary>
    /// SQL Server's <c>time</c> column only holds 00:00:00 through 23:59:59.9999999 — a stage
    /// offset can't be stored as a raw TimeSpan if it wraps past midnight. The persisted
    /// <see cref="MedicationReminderLog.ScheduledTime"/> is clamped to the edge of the day in
    /// that rare case; the actual delivery instant (computed from the UTC anchor above) is
    /// unaffected and still fires exactly 10 minutes before/after the reminder time.
    /// </summary>
    private static TimeSpan ClampToDay(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            return TimeSpan.Zero;

        var oneDay = TimeSpan.FromDays(1);
        return time >= oneDay ? oneDay - TimeSpan.FromTicks(1) : time;
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
