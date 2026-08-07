using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Single source of truth for turning a <see cref="MedicineReminder"/> into today's
/// scheduled work (reminder log + background job).
/// </summary>
public interface IMedicationSchedulingService
{
    /// <summary>
    /// Materialises today's <see cref="MedicationReminderLog"/> for a reminder that applies
    /// today and has not been recorded yet.
    ///
    /// A dose whose time is still ahead (or only just past) also gets its delivery job queued.
    /// A dose whose time passed long ago is still recorded — as Overdue, with no notification —
    /// so that Today's Schedule, adherence and history cover every planned dose of the day.
    /// </summary>
    /// <returns><c>true</c> if a log was created; otherwise <c>false</c>.</returns>
    Task<bool> ScheduleTodayIfApplicableAsync(MedicineReminder reminder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pure, read-only calculation of today's upcoming reminder occurrences for a given reminder.
    /// Does NOT insert logs, queue jobs, publish events, or modify the database in any way.
    /// Used exclusively by the GET /api/medication-reminders/upcoming endpoint.
    /// </summary>
    Task<IReadOnlyCollection<(MedicationReminderLog Log, DateTime ScheduledUtc, bool Notify)>> GetUpcomingStagesAsync(
        MedicineReminder reminder,
        CancellationToken cancellationToken = default);
}
