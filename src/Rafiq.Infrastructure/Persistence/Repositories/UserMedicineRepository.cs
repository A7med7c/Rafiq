using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class UserMedicineRepository : IUserMedicineRepository
{
    private readonly RafiqDbContext _context;

    public UserMedicineRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(UserMedicine userMedicine, CancellationToken cancellationToken = default)
        => _context.UserMedicines
            .AddAsync(userMedicine, cancellationToken)
            .AsTask();

    public Task<UserMedicine?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.UserMedicines
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<UserMedicine>> GetAllByProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.UserMedicines
            .Where(m => m.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(
        Guid userHealthProfileId,
        string medicineName,
        string dosage,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName   = medicineName.Trim().ToLower();
        var normalizedDosage = dosage.Trim().ToLower();

        return _context.UserMedicines
            .Where(m => m.UserHealthProfileId == userHealthProfileId
                     && !m.IsDeleted
                     && (excludeId == null || m.Id != excludeId.Value))
            .AnyAsync(
                m => m.MedicineName.ToLower() == normalizedName
                  && m.Dosage.ToLower()       == normalizedDosage,
                cancellationToken);
    }
    
    public async Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sameProfile = await _context.UserMedicines
            .Where(r => r.FileHash == fileHash && r.UserHealthProfileId == currentProfileId && !r.IsDeleted)
            .Select(r => new { r.UserHealthProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (sameProfile != null)
            return (sameProfile.UserHealthProfileId, string.Empty, true);

        var familyDuplicate = await (from r in _context.UserMedicines
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

    public void Update(UserMedicine userMedicine)
    {
        _context.UserMedicines.Update(userMedicine);
    }

    public void Delete(UserMedicine userMedicine)
    {
        userMedicine.SoftDelete();
        _context.UserMedicines.Update(userMedicine);
    }
}
