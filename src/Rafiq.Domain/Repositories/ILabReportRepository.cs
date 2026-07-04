using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface ILabReportRepository
{
    Task AddAsync(LabReport labReport, CancellationToken cancellationToken = default);

    Task<LabReport?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabReport>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
