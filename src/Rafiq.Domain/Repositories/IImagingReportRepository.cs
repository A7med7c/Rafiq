using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IImagingReportRepository
{
    Task AddAsync(ImagingReport imagingReport, CancellationToken cancellationToken = default);

    Task<ImagingReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImagingReport>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a non-deleted imaging report already exists for the same profile
    /// with identical report date, imaging type, and body part.
    /// </summary>
    Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        DateOnly reportDate,
        string imagingType,
        string bodyPart,
        CancellationToken cancellationToken = default);
        
    Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    void Update(ImagingReport imagingReport);

    void Remove(ImagingReport imagingReport);
}
