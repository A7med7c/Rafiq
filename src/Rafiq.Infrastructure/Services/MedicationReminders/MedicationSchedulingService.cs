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
/// The one place that decides whether a reminder is due today, materialises its logs,
/// and hands the delivery jobs to Hangfire. Both the daily scheduler and the
/// create-reminder flow go through here, so a reminder created after the daily sweep
/// still fires today.
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

    /// <summary>
    /// Minutes between successive escalation attempts for one dose occurrence.
    /// The configured ReminderTime is the dose time (middle attempt).
    ///
    /// Attempt 1 fires StageIntervalMinutes before the dose time (early warning).
    /// Attempt 2 fires at the configured dose time (main reminder).
    /// Attempt 3 fires StageIntervalMinutes after the dose time (overdue reminder).
    ///
    /// Example — configured dose time 8:00 PM (StageIntervalMinutes = 10):
    ///   Attempt 1 → 7:50 PM  (early warning)
    ///   Attempt 2 → 8:00 PM  (main reminder, at configured dose time)
    ///   Attempt 3 → 8:10 PM  (overdue reminder)
    /// </summary>
    private const int StageIntervalMinutes = 1;

    /// <summary>
    /// Pure calculation of upcoming reminder occurrences for today.
    /// Returns a collection of tuples (log, scheduled UTC, notify?) without persisting or scheduling.
    /// Used by the GET upcoming endpoint (read‑only).
    /// </summary>
    public async Task<IReadOnlyCollection<(MedicationReminderLog Log, DateTime ScheduledUtc, bool Notify)>> GetUpcomingStagesAsync(
        MedicineReminder reminder,
        CancellationToken cancellationToken = default)
    {
        var today = dateTimeProvider.Today;
        if (!IsApplicableForDate(reminder, today))
            return Array.Empty<(MedicationReminderLog, DateTime, bool)>();

        var profileId = await ResolveHealthProfileIdAsync(reminder, cancellationToken);
        if (profileId is null)
            return Array.Empty<(MedicationReminderLog, DateTime, bool)>();

        return CalculateUpcomingOccurrences(reminder, today, profileId.Value);
    }

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

        // The anchor is the UTC instant that corresponds to the user's configured reminder
        // time on today's date in the reminder timezone.  All three attempts are offset from
        // this single anchor so that midnight-crossing is handled correctly at the UTC level
        // rather than by manipulating wall-clock TimeSpans.
        var anchorUtc = dateTimeProvider.ToUtc(today, reminder.ReminderTime);

        // All three logs must be committed before any job is queued: with a zero delay
        // Hangfire can run MedicationReminderJob immediately, and it looks the log up by id.
        //
        // ExistsForDateAsync above is only an optimistic pre-check — it cannot stop two
        // concurrent callers (e.g. the daily sweep racing a manual create) from both passing
        // it and both reaching this insert.  The unique filtered index on
        // (MedicineReminderId, ScheduledDate, ReminderNumber) is the actual guarantee: the
        // loser's insert fails here, and that failure is the signal that someone else already
        // scheduled this occurrence — not a real error, so no jobs get queued for it.

        var stages = CalculateUpcomingOccurrences(reminder, today, profileId.Value);

        foreach (var (log, _, _) in stages)
        {
            await logRepository.AddAsync(log, cancellationToken);
        }

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
                    "Recorded reminder {ReminderId} stage #{Number} (log {LogId}) as Overdue: " +
                    "it was due at {ScheduledUtc:o}, beyond the {Grace} grace window. No notification scheduled.",
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
    /// SQL Server's <c>time</c> column only holds 00:00:00–23:59:59.9999999.
    /// A stage offset applied to a reminder time near midnight can produce a value that
    /// overflows (e.g. 23:50 + 20 min = 24:10).  The persisted ScheduledTime is clamped to
    /// the day boundary in that case; the actual delivery instant computed from the UTC anchor
    /// is unaffected and still fires at the correct wall-clock time.
    /// </summary>
    private static TimeSpan ClampToDay(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            return TimeSpan.Zero;

        var oneDay = TimeSpan.FromDays(1);
        return time >= oneDay ? oneDay - TimeSpan.FromTicks(1) : time;
    }

    /// <param name="date">A date in the reminder's timezone, not a UTC date.</param>
    private static bool IsApplicableForDate(MedicineReminder reminder, DateOnly date)
    {
        if (!reminder.IsEnabled || reminder.IsDeleted)
            return false;

        if (date < reminder.StartDate || date > reminder.EndDate)
            return false;

        return reminder.RepeatType switch
        {
            RepeatType.Once    => reminder.StartDate == date,
            RepeatType.Daily   => true,
            RepeatType.Weekly  => (date.DayNumber - reminder.StartDate.DayNumber) % 7 == 0,
            RepeatType.Monthly => date.Day == reminder.StartDate.Day,
            _                  => false
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

    /// <summary>
    /// Pure calculation — no I/O, no side effects.
    /// Produces the three escalation-stage tuples (log shell, UTC fire time, should-notify?)
    /// for a reminder that applies on <paramref name="date"/>.
    ///
    /// The returned <see cref="MedicationReminderLog"/> objects are NOT attached to any
    /// DbContext and are NOT saved. Callers that need persistence must call AddAsync
    /// themselves; callers that only need the schedule (e.g. the GET endpoint) can discard
    /// the log objects entirely.
    /// </summary>
    private IReadOnlyCollection<(MedicationReminderLog Log, DateTime ScheduledUtc, bool Notify)>
        CalculateUpcomingOccurrences(MedicineReminder reminder, DateOnly date, Guid profileId)
    {
        var anchorUtc = dateTimeProvider.ToUtc(date, reminder.ReminderTime);

        var stageDefinitions = new (int ReminderNumber, TimeSpan Offset)[]
        {
            (1, TimeSpan.FromMinutes(-StageIntervalMinutes)),  // early warning
            (2, TimeSpan.Zero),                                 // main reminder
            (3, TimeSpan.FromMinutes(StageIntervalMinutes)),   // overdue reminder
        };

        var results = new List<(MedicationReminderLog, DateTime, bool)>(3);

        foreach (var (reminderNumber, offset) in stageDefinitions)
        {
            var stageUtc = anchorUtc + offset;
            var delay    = stageUtc - dateTimeProvider.UtcNow;
            var notify   = delay >= -_lateGrace;

            var log = new MedicationReminderLog(
                reminder.Id,
                profileId,
                date,
                ClampToDay(reminder.ReminderTime + offset),
                reminderNumber);

            if (!notify)
                log.MarkAsOverdue();

            results.Add((log, stageUtc, notify));
        }

        return results;
    }
}
