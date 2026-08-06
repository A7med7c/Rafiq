using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface ILabReportRepository
{
    Task AddAsync(LabReport labReport, CancellationToken cancellationToken = default);

    Task<LabReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabReport>> GetAllByProfileIdAsync(Guid userHealthProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a non-deleted lab report already exists for the same profile
    /// with identical report date, lab name, and doctor name.
    /// </summary>
    Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        DateOnly reportDate,
        string labName,
        string doctorName,
        CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Searches for an existing lab report by its file hash, checking both the current profile
    /// and any other profile the current user has Active access to.
    /// </summary>
    Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    void Update(LabReport labReport);

    void Remove(LabReport labReport);
}
