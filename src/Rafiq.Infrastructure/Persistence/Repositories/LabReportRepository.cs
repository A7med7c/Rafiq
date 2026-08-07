using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class LabReportRepository : ILabReportRepository
{
    private readonly RafiqDbContext _context;

    public LabReportRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(LabReport labReport, CancellationToken cancellationToken = default)
        => _context.LabReports
            .AddAsync(labReport, cancellationToken)
            .AsTask();

    public Task<LabReport?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.LabReports
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LabReport>> GetAllByProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.LabReports
            .Include(r => r.Results)
            .Where(r => r.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    
    public Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        DateOnly reportDate,
        string labName,
        string doctorName,
        CancellationToken cancellationToken = default)
        => _context.LabReports.AnyAsync(
            r => r.UserHealthProfileId == userHealthProfileId
                 && !r.IsDeleted
                 && r.ReportDate == reportDate
                 && r.LabName.ToLower() == labName.ToLower()
                 && r.DoctorName.ToLower() == doctorName.ToLower(),
            cancellationToken);
            
    public async Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sameProfile = await _context.LabReports
            .Where(r => r.FileHash == fileHash && r.UserHealthProfileId == currentProfileId && !r.IsDeleted)
            .Select(r => new { r.UserHealthProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (sameProfile != null)
            return (sameProfile.UserHealthProfileId, string.Empty, true);

        var familyDuplicate = await (from r in _context.LabReports
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

    public void Update(LabReport labReport)
        => _context.LabReports.Update(labReport);

    public void Remove(LabReport labReport)
        => _context.LabReports.Remove(labReport);
}
