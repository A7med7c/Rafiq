using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IPrescriptionRepository
{
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);

    Task<Prescription?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Prescription>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
