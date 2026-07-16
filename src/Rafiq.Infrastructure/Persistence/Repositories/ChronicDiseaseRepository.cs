using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class ChronicDiseaseRepository : IChronicDiseaseRepository
{
    private readonly RafiqDbContext _dbContext;

    public ChronicDiseaseRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChronicDisease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.ChronicDiseases
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameForProfileAsync(
        Guid profileId, string name, Guid? excludeId, CancellationToken cancellationToken = default)
        => _dbContext.ChronicDiseases
            .AnyAsync(x =>
                x.UserHealthProfileId == profileId &&
                x.Name.ToLower() == name.ToLower() &&
                (excludeId == null || x.Id != excludeId),
            cancellationToken);

    public Task AddAsync(ChronicDisease disease, CancellationToken cancellationToken = default)
        => _dbContext.ChronicDiseases.AddAsync(disease, cancellationToken).AsTask();

    public void Remove(ChronicDisease disease)
        => _dbContext.ChronicDiseases.Remove(disease);
}
