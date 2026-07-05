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
        Guid userId,
        CancellationToken cancellationToken = default)
        => _context.UserMedicines
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<UserMedicine>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.UserMedicines
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var userMedicine = await _context.UserMedicines
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken);

        if (userMedicine is null)
            return false;

        userMedicine.SoftDelete();
        return true;
    }
}
