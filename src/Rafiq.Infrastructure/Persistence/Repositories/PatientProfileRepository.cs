using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class PatientProfileRepository : IPatientProfileRepository
{
    private readonly RafiqDbContext _dbContext;

    public PatientProfileRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PatientProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.PatientProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PatientProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.PatientProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.PatientProfiles.AnyAsync(x => x.UserId == userId, cancellationToken);

    public Task AddAsync(PatientProfile patientProfile, CancellationToken cancellationToken = default)
        => _dbContext.PatientProfiles.AddAsync(patientProfile, cancellationToken).AsTask();

    public void Update(PatientProfile patientProfile)
        => _dbContext.PatientProfiles.Update(patientProfile);

    public void Remove(PatientProfile patientProfile)
        => _dbContext.PatientProfiles.Remove(patientProfile);
}
