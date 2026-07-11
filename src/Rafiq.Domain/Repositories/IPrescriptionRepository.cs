using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IPrescriptionRepository
{
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);

    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Prescription>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);

    Task<List<PrescriptionMedicine>> GetMedicinesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    void Update(Prescription prescription);
    
    void Delete(Prescription prescription);
}
