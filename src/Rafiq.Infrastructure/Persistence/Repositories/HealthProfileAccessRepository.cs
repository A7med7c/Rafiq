using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class HealthProfileAccessRepository : IHealthProfileAccessRepository
{
    private readonly RafiqDbContext _dbContext;

    public HealthProfileAccessRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<HealthProfileAccess?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _dbContext.HealthProfileAccesses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<HealthProfileAccess?> GetActiveOwnerAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => _dbContext.HealthProfileAccesses
            .FirstOrDefaultAsync(
                x => x.UserHealthProfileId == userHealthProfileId
                    && x.GranteeUserId == granteeUserId
                    && x.Role == AccessRole.Owner
                    && x.Status == AccessStatus.Active,
                cancellationToken);

    public Task<HealthProfileAccess?> GetPendingOrActiveAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => _dbContext.HealthProfileAccesses
            .FirstOrDefaultAsync(
                x => x.UserHealthProfileId == userHealthProfileId
                    && x.GranteeUserId == granteeUserId
                    && (x.Status == AccessStatus.Pending || x.Status == AccessStatus.Active),
                cancellationToken);

    public async Task<IReadOnlyList<HealthProfileAccess>> GetPendingReceivedInvitationsAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => await _dbContext.HealthProfileAccesses
            .AsNoTracking()
            .Include(x => x.UserHealthProfile)
            .Where(x => x.GranteeUserId == granteeUserId
                && x.Status == AccessStatus.Pending
                && x.Origin == AccessOrigin.GrantInvitation)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HealthProfileAccess>> GetPendingReceivedAccessRequestsAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _dbContext.HealthProfileAccesses
            .AsNoTracking()
            .Where(x => x.UserHealthProfileId == userHealthProfileId
                && x.Status == AccessStatus.Pending
                && x.Origin == AccessOrigin.AccessRequest)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HealthProfileAccess>> GetActiveAccessibleProfilesAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => await _dbContext.HealthProfileAccesses
            .AsNoTracking()
            .Include(x => x.UserHealthProfile)
            .Where(x => x.GranteeUserId == granteeUserId && x.Status == AccessStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<HealthProfileAccess?> GetActiveAccessAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => _dbContext.HealthProfileAccesses
            .FirstOrDefaultAsync(
                x => x.UserHealthProfileId == userHealthProfileId
                    && x.GranteeUserId == granteeUserId
                    && x.Status == AccessStatus.Active,
                cancellationToken);

    public async Task<IReadOnlyList<HealthProfileAccess>> GetActiveMembersAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => await _dbContext.HealthProfileAccesses
            .AsNoTracking()
            .Where(x => x.UserHealthProfileId == userHealthProfileId && x.Status == AccessStatus.Active)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveOwnersAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default)
        => _dbContext.HealthProfileAccesses
            .CountAsync(
                x => x.UserHealthProfileId == userHealthProfileId
                    && x.Role == AccessRole.Owner
                    && x.Status == AccessStatus.Active,
                cancellationToken);

    public async Task<IReadOnlyList<HealthProfileAccess>> GetSentInvitationsAsync(
        Guid inviterUserId,
        CancellationToken cancellationToken = default)
        => await _dbContext.HealthProfileAccesses
            .AsNoTracking()
            .Include(x => x.UserHealthProfile)
            .Where(x => x.InvitedByUserId == inviterUserId
                && x.Origin == AccessOrigin.GrantInvitation)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(HealthProfileAccess healthProfileAccess, CancellationToken cancellationToken = default)
        => _dbContext.HealthProfileAccesses.AddAsync(healthProfileAccess, cancellationToken).AsTask();

    public void Update(HealthProfileAccess healthProfileAccess)
        => _dbContext.HealthProfileAccesses.Update(healthProfileAccess);
}
