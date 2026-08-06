using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class ImagingReportRepository : IImagingReportRepository
{
    private readonly RafiqDbContext _context;

    public ImagingReportRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(ImagingReport imagingReport, CancellationToken cancellationToken = default)
        => _context.ImagingReports
            .AddAsync(imagingReport, cancellationToken)
            .AsTask();

    public Task<ImagingReport?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.ImagingReports
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ImagingReport>> GetAllByProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.ImagingReports
            .Where(r => r.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    
    public Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        DateOnly reportDate,
        string imagingType,
        string bodyPart,
        CancellationToken cancellationToken = default)
        => _context.ImagingReports.AnyAsync(
            r => r.UserHealthProfileId == userHealthProfileId
                 && !r.IsDeleted
                 && r.ReportDate == reportDate
                 && r.ImagingType.ToLower() == imagingType.ToLower()
                 && r.BodyPart.ToLower() == bodyPart.ToLower(),
            cancellationToken);

    public async Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sameProfile = await _context.ImagingReports
            .Where(r => r.FileHash == fileHash && r.UserHealthProfileId == currentProfileId && !r.IsDeleted)
            .Select(r => new { r.UserHealthProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (sameProfile != null)
            return (sameProfile.UserHealthProfileId, string.Empty, true);

        var familyDuplicate = await (from r in _context.ImagingReports
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

    public void Update(ImagingReport imagingReport)
        => _context.ImagingReports.Update(imagingReport);

    public void Remove(ImagingReport imagingReport)
        => _context.ImagingReports.Remove(imagingReport);
}
