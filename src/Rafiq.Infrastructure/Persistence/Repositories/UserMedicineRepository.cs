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
