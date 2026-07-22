using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories;

public interface IChronicDiseaseRepository
{
    Task<ChronicDisease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameForProfileAsync(Guid profileId, string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(ChronicDisease disease, CancellationToken cancellationToken = default);
    void Remove(ChronicDisease disease);
}
