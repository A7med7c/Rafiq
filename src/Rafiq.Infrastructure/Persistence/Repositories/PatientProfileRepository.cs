using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class UserHealthProfileRepository : IPatientProfileRepository
{
    private readonly RafiqDbContext _dbContext;

    public UserHealthProfileRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserHealthProfile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _dbContext.UserHealthProfiles
            .Include(x => x.Allergies)
            .Include(x => x.ChronicDiseases)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<UserHealthProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _dbContext.UserHealthProfiles
            .Include(x => x.Allergies)
            .Include(x => x.ChronicDiseases)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.UserHealthProfiles.AnyAsync(x => x.UserId == userId, cancellationToken);

    public Task AddAsync(UserHealthProfile UserHealthProfile, CancellationToken cancellationToken = default)
        => _dbContext.UserHealthProfiles.AddAsync(UserHealthProfile, cancellationToken).AsTask();

    public void Update(UserHealthProfile UserHealthProfile)
        => _dbContext.UserHealthProfiles.Update(UserHealthProfile);

    public void Remove(UserHealthProfile UserHealthProfile)
        => _dbContext.UserHealthProfiles.Remove(UserHealthProfile);
}
