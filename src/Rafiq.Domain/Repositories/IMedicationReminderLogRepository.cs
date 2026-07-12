using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IMedicationReminderLogRepository
{
    Task AddAsync(MedicationReminderLog log, CancellationToken cancellationToken = default);
    Task<MedicationReminderLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MedicationReminderLog?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(Guid medicineReminderId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<MedicationReminderLog>> GetTodayByProfileIdAsync(Guid userHealthProfileId, DateOnly today, CancellationToken cancellationToken = default);
    Task<List<MedicationReminderLog>> GetPendingSubsequentLogsAsync(Guid medicineReminderId, DateOnly date, int afterReminderNumber, CancellationToken cancellationToken = default);
    void Update(MedicationReminderLog log);
}
