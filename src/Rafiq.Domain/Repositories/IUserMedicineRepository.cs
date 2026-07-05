using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IUserMedicineRepository
{
    Task AddAsync(UserMedicine userMedicine, CancellationToken cancellationToken = default);

    Task<UserMedicine?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserMedicine>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
