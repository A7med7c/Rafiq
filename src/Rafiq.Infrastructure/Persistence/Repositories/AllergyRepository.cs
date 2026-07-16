using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class AllergyRepository : IAllergyRepository
{
    private readonly RafiqDbContext _dbContext;

    public AllergyRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Allergy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Allergies
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameForProfileAsync(
        Guid profileId, string name, Guid? excludeId, CancellationToken cancellationToken = default)
        => _dbContext.Allergies
            .AnyAsync(x =>
                x.UserHealthProfileId == profileId &&
                x.Name.ToLower() == name.ToLower() &&
                (excludeId == null || x.Id != excludeId),
            cancellationToken);

    public Task AddAsync(Allergy allergy, CancellationToken cancellationToken = default)
        => _dbContext.Allergies.AddAsync(allergy, cancellationToken).AsTask();

    public void Remove(Allergy allergy)
        => _dbContext.Allergies.Remove(allergy);
}
