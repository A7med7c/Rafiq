using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Domain.Repositories;

public interface IDocumentUploadSessionRepository
{
    Task<DocumentUploadSession?> GetByImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
    void Remove(DocumentUploadSession session);
    Task AddAsync(DocumentUploadSession session, CancellationToken cancellationToken = default);
}
