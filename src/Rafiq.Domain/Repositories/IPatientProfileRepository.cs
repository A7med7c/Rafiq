using Rafiq.Domain.Entities;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories;

public interface IPatientProfileRepository
{
    Task<UserHealthProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserHealthProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserHealthProfile patientProfile, CancellationToken cancellationToken = default);
    void Update(UserHealthProfile patientProfile);
    void Remove(UserHealthProfile patientProfile);
}
