public interface IGeneralDocumentRepository
{
    Task AddAsync(
        GeneralDocument document,
        CancellationToken cancellationToken = default);

    Task<GeneralDocument?> GetByIdAsync(
        Guid id,
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a document by ID without scoping to a profile — used by background jobs.</summary>
    Task<GeneralDocument?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneralDocument>> GetAllByUserIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a non-deleted general document already exists for the same profile
    /// with an identical title (case-insensitive).
    /// </summary>
    Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        string title,
        CancellationToken cancellationToken = default);
        
    Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    void Update(GeneralDocument document);

    void Remove(GeneralDocument document);
}
