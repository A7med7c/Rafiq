using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class PrescriptionRepository : IPrescriptionRepository
{
    private readonly RafiqDbContext _context;

    public PrescriptionRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default)
        => _context.Prescriptions
            .AddAsync(prescription, cancellationToken)
            .AsTask();

    public Task<Prescription?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Prescriptions
            .Include(p => p.Medicines)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Prescription>> GetAllByProfileIdAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _context.Prescriptions
            .Include(p => p.Medicines)
            .Where(p => p.UserHealthProfileId == userHealthProfileId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsDuplicateAsync(
        Guid userHealthProfileId,
        DateOnly prescriptionDate,
        string doctorName,
        string patientName,
        CancellationToken cancellationToken = default)
        => _context.Prescriptions.AnyAsync(
            p => p.UserHealthProfileId == userHealthProfileId
                 && !p.IsDeleted
                 && p.PrescriptionDate == prescriptionDate
                 && p.DoctorName.ToLower() == doctorName.ToLower()
                 && p.PatientName.ToLower() == patientName.ToLower(),
            cancellationToken);

    public async Task<(Guid profileId, string profileName, bool isSameProfile)?> FindDuplicateByHashAsync(
        string fileHash,
        Guid currentProfileId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sameProfile = await _context.Prescriptions
            .Where(r => r.FileHash == fileHash && r.UserHealthProfileId == currentProfileId && !r.IsDeleted)
            .Select(r => new { r.UserHealthProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (sameProfile != null)
            return (sameProfile.UserHealthProfileId, string.Empty, true);

        var familyDuplicate = await (from r in _context.Prescriptions
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

    public void Update(Prescription prescription)
    {
        _context.Prescriptions.Update(prescription);
    }

    public void Delete(Prescription prescription)
    {
        prescription.SoftDelete();
        _context.Prescriptions.Update(prescription);
    }

    public async Task<List<PrescriptionMedicine>> GetMedicinesByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _context.PrescriptionMedicines
            .Include(m => m.Prescription)
            .Where(m => ids.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
