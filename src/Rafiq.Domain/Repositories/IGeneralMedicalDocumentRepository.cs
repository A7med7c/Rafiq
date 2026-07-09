public interface IGeneralDocumentRepository
{
    Task AddAsync(
        GeneralDocument document,
        CancellationToken cancellationToken = default);

    Task<GeneralDocument?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneralDocument>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void Remove(GeneralDocument document);
}