using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories;

public interface IEmergencyContactRepository
{
    Task AddAsync(EmergencyContact emergencyContact, CancellationToken cancellationToken = default);

    Task<EmergencyContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmergencyContact>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Update(EmergencyContact emergencyContact);

    void Delete(EmergencyContact emergencyContact);
}
