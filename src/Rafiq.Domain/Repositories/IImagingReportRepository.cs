using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IImagingReportRepository
{
    Task AddAsync(ImagingReport imagingReport, CancellationToken cancellationToken = default);

    Task<ImagingReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImagingReport>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);
    
    void Update(ImagingReport imagingReport);
    
    void Remove(ImagingReport imagingReport);
}
