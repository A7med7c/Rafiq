using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class DocumentUploadSessionRepository(RafiqDbContext dbContext) : IDocumentUploadSessionRepository
{
    public Task<DocumentUploadSession?> GetByImageUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        return dbContext.DocumentUploadSessions
            .FirstOrDefaultAsync(s => s.ImageUrl == imageUrl, cancellationToken);
    }

    public void Remove(DocumentUploadSession session)
    {
        dbContext.DocumentUploadSessions.Remove(session);
    }

    public async Task AddAsync(DocumentUploadSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.DocumentUploadSessions.AddAsync(session, cancellationToken);
    }
}
