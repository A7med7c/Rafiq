using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface ILabReportRepository
{
    Task AddAsync(LabReport labReport, CancellationToken cancellationToken = default);

    Task<LabReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabReport>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);
}
