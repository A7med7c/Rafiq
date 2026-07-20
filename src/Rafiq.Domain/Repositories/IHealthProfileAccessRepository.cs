using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Repositories;

public interface IHealthProfileAccessRepository
{
    /// <summary>
    /// Returns the distinct ApplicationUser ids that currently hold <see cref="AccessStatus.Active"/>
    /// access to the profile in one of the supplied roles. Soft-deleted access records are excluded
    /// by the global query filter, and <c>Distinct</c> collapses duplicate access records so each
    /// user is returned only once. Used to route reminders and activity notifications to the right
    /// managers/members of a Managed Profile without sending duplicates.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveGranteeUserIdsByRolesAsync(
        Guid userHealthProfileId,
        IReadOnlyCollection<AccessRole> roles,
        CancellationToken cancellationToken = default);

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

    Task<IReadOnlyList<HealthProfileAccess>> GetSentInvitationsAsync(
        Guid inviterUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(HealthProfileAccess healthProfileAccess, CancellationToken cancellationToken = default);

    void Update(HealthProfileAccess healthProfileAccess);

    Task NullifyInvitedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task HardDeleteByGranteeUserIdAsync(Guid granteeUserId, CancellationToken cancellationToken = default);
}
