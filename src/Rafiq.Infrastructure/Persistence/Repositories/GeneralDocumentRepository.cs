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

    public Task<GeneralDocument?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.GeneralDocuments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GeneralDocument>> GetAllByUserIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.GeneralDocuments
            .Where(x => x.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    
    public Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        string title,
        CancellationToken cancellationToken = default)
        => _context.GeneralDocuments.AnyAsync(
            d => d.UserHealthProfileId == userHealthProfileId
                 && !d.IsDeleted
                 && d.Title.ToLower() == title.ToLower(),
            cancellationToken);

    public void Update(GeneralDocument document)
    {
        _context.GeneralDocuments.Update(document);
    }

    public void Remove(GeneralDocument document)
    {
        _context.GeneralDocuments.Remove(document);
    }
}
