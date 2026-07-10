using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IUserMedicineRepository
{
    Task AddAsync(UserMedicine userMedicine, CancellationToken cancellationToken = default);

    Task<UserMedicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserMedicine>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);

    void Update(UserMedicine userMedicine);

    void Delete(UserMedicine userMedicine);
}
