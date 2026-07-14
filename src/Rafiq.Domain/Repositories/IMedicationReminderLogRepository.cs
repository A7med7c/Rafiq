using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IMedicationReminderLogRepository
{
    Task AddAsync(MedicationReminderLog log, CancellationToken cancellationToken = default);
    Task<MedicationReminderLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicationReminderLog?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(Guid medicineReminderId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<MedicationReminderLog>> GetTodayByProfileIdAsync(Guid userHealthProfileId, DateOnly today, CancellationToken cancellationToken = default);


    /// <summary>
    /// Returns all Pending logs that belong to the same dose occurrence as
    /// <paramref name="confirmedLogId"/> but are not that log itself.
    ///
    /// An occurrence is identified by (MedicineReminderId, ScheduledDate), which the
    /// unique filtered index guarantees maps to exactly one set of attempt logs.
    ///
    /// Only Pending logs are returned: Sent logs have already delivered their notification
    /// and their Hangfire jobs have finished — there is nothing left to cancel on them.
    /// </summary>
    Task<List<MedicationReminderLog>> GetPendingOtherLogsAsync(
        Guid medicineReminderId,
        DateOnly date,
        Guid confirmedLogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all Pending and Overdue logs for a reminder occurrence on a given date.
    /// Used by the update flow to cancel every not-yet-confirmed attempt from the old schedule,
    /// including Overdue logs whose time already passed without a Hangfire job firing.
    /// </summary>
    Task<List<MedicationReminderLog>> GetPendingAndOverdueLogsAsync(
        Guid medicineReminderId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all Stage-3 (ReminderNumber == 3) logs in Sent status whose SentAt
    /// is older than <paramref name="cutoff"/> and that have no Confirmed sibling log
    /// for the same (MedicineReminderId, ScheduledDate) occurrence.
    /// Includes MedicineReminder → UserMedicine and UserHealthProfile navigations
    /// required by the missed-medication escalation flow.
    /// </summary>
    Task<List<MedicationReminderLog>> GetSentStage3LogsOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);

    void Update(MedicationReminderLog log);
}
