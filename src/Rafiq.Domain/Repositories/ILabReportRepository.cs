using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface ILabReportRepository
{
    Task AddAsync(LabReport labReport, CancellationToken cancellationToken = default);

    Task<LabReport?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LabReport>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the Id of the DocumentType with the given name.
    /// Creates the DocumentType if it does not already exist.
    /// </summary>
    Task<Guid> GetOrCreateDocumentTypeIdAsync(string name, CancellationToken cancellationToken = default);
}
