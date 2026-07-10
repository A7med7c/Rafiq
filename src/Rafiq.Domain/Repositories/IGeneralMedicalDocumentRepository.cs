public interface IGeneralDocumentRepository
{
    Task AddAsync(
        GeneralDocument document,
        CancellationToken cancellationToken = default);

    Task<GeneralDocument?> GetByIdAsync(
        Guid id,
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneralDocument>> GetAllByUserIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);
    
    void Update(GeneralDocument document);

    void Remove(GeneralDocument document);
}
