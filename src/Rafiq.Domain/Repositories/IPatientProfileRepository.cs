using Rafiq.Domain.Entities;

namespace Rafiq.Domain.Repositories;

public interface IPatientProfileRepository
{
    Task<PatientProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(PatientProfile patientProfile, CancellationToken cancellationToken = default);
    void Update(PatientProfile patientProfile);
    void Remove(PatientProfile patientProfile);
}
