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

    public async Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sameProfile = await _context.GeneralDocuments
            .Where(r => r.FileHash == fileHash && r.UserHealthProfileId == currentProfileId && !r.IsDeleted)
            .Select(r => new { r.UserHealthProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (sameProfile != null)
            return (sameProfile.UserHealthProfileId, string.Empty, true);

        var familyDuplicate = await (from r in _context.GeneralDocuments
            join hpa in _context.HealthProfileAccesses on r.UserHealthProfileId equals hpa.UserHealthProfileId
            join p in _context.UserHealthProfiles on r.UserHealthProfileId equals p.Id
            where r.FileHash == fileHash
               && !r.IsDeleted
               && hpa.GranteeUserId == currentUserId
               && hpa.Status == Rafiq.Domain.Enums.AccessStatus.Active
            select new { r.UserHealthProfileId, ProfileName = p.FirstName + " " + p.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (familyDuplicate != null)
            return (familyDuplicate.UserHealthProfileId, familyDuplicate.ProfileName, false);

        return null;
    }

    public void Update(GeneralDocument document)
    {
        _context.GeneralDocuments.Update(document);
    }

    public void Remove(GeneralDocument document)
    {
        _context.GeneralDocuments.Remove(document);
    }
}
