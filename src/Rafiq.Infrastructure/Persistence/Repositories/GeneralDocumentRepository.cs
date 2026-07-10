using Microsoft.EntityFrameworkCore;
using Rafiq.Infrastructure.Persistence;

public sealed class GeneralDocumentRepository
    : IGeneralDocumentRepository
{
    private readonly RafiqDbContext _context;

    public GeneralDocumentRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(
        GeneralDocument document,
        CancellationToken cancellationToken = default)
        => _context.GeneralDocuments
            .AddAsync(document, cancellationToken)
            .AsTask();

    public Task<GeneralDocument?> GetByIdAsync(
        Guid id,
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => _context.GeneralDocuments
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserHealthProfileId == userHealthProfileId,
                cancellationToken);

    public async Task<IReadOnlyList<GeneralDocument>> GetAllByUserIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.GeneralDocuments
            .Where(x => x.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Remove(GeneralDocument document)
    {
        _context.GeneralDocuments.Remove(document);
    }
}
