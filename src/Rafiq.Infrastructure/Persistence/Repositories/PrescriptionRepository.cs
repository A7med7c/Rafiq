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
        Guid userId,
        CancellationToken cancellationToken = default)
        => _context.Prescriptions
            .Include(p => p.Medicines)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Prescription>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.Prescriptions
            .Include(p => p.Medicines)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);

        if (prescription is null)
            return false;

        prescription.SoftDelete();
        return true;
    }

    public async Task<List<PrescriptionMedicine>> GetMedicinesByIdsAsync(
        IEnumerable<Guid> ids, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.PrescriptionMedicines
            .Include(m => m.Prescription)
            .Where(m => ids.Contains(m.Id) && m.Prescription.UserId == userId && !m.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
