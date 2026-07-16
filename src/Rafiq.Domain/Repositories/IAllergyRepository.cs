using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories;

public interface IAllergyRepository
{
    Task<Allergy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameForProfileAsync(Guid profileId, string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task AddAsync(Allergy allergy, CancellationToken cancellationToken = default);
    void Remove(Allergy allergy);
}
