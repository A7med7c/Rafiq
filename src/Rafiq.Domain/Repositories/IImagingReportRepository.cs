using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IImagingReportRepository
{
    Task AddAsync(ImagingReport imagingReport, CancellationToken cancellationToken = default);

    Task<ImagingReport?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImagingReport>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
