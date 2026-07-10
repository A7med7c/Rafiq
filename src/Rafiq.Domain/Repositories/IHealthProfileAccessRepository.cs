using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories;

public interface IHealthProfileAccessRepository
{
    Task<HealthProfileAccess?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<HealthProfileAccess?> GetActiveOwnerAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    Task<HealthProfileAccess?> GetPendingOrActiveAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthProfileAccess>> GetPendingReceivedInvitationsAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthProfileAccess>> GetPendingReceivedAccessRequestsAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthProfileAccess>> GetActiveAccessibleProfilesAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    Task<HealthProfileAccess?> GetActiveAccessAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HealthProfileAccess>> GetActiveMembersAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveOwnersAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task AddAsync(HealthProfileAccess healthProfileAccess, CancellationToken cancellationToken = default);

    void Update(HealthProfileAccess healthProfileAccess);
}
