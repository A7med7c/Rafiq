using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IMedicineReminderRepository
{
    Task<MedicineReminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MedicineReminder>> GetByUserMedicineIdAsync(Guid userMedicineId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<MedicineReminder> reminders, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userMedicineId, TimeSpan time, DateOnly startDate, DateOnly endDate, string repeatType, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<List<MedicineReminder>> GetActiveForDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all non-deleted medicine reminders for medicines belonging to the given
    /// health profile, with the <see cref="MedicineReminder.UserMedicine"/> navigation
    /// property included (for the medicine name).
    /// </summary>
    Task<List<MedicineReminder>> GetAllWithMedicineByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    void Update(MedicineReminder reminder);
    void Delete(MedicineReminder reminder);
}
