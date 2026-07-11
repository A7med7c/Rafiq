using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class EmergencyContactRepository : IEmergencyContactRepository
{
    private readonly RafiqDbContext _context;

    public EmergencyContactRepository(RafiqDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(EmergencyContact emergencyContact, CancellationToken cancellationToken = default)
        => _context.EmergencyContacts
            .AddAsync(emergencyContact, cancellationToken)
            .AsTask();

    public Task<EmergencyContact?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.EmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<EmergencyContact>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.EmergencyContacts
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Update(EmergencyContact emergencyContact)
    {
        _context.EmergencyContacts.Update(emergencyContact);
    }

    public void Delete(EmergencyContact emergencyContact)
    {
        emergencyContact.SoftDelete();
        _context.EmergencyContacts.Update(emergencyContact);
    }
}
