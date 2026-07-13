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
    /// Returns stage-3 logs that are still in <see cref="MedicationReminderStatus.Sent"/>
    /// and whose <see cref="MedicationReminderLog.SentAt"/> is on or before
    /// <paramref name="cutoff"/> with no sibling log for the same dose occurrence that
    /// has been confirmed. Used by the missed-medication escalation background service.
    /// </summary>
    Task<List<MedicationReminderLog>> GetSentStage3LogsOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);

    void Update(MedicationReminderLog log);
}
