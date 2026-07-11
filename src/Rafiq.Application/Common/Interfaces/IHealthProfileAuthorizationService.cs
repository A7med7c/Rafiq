using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Centralizes "does the current user have Active access to this health profile, and
/// what can they do with it?" so medical-data features don't each re-query
/// HealthProfileAccess and re-implement the Owner/Manager/Viewer role matrix.
/// </summary>
public interface IHealthProfileAuthorizationService
{
    Task<HealthProfileAccessContext> GetAccessAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task<HealthProfileAccessContext> EnsureCanReadAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task<HealthProfileAccessContext> EnsureCanWriteAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);

    Task<HealthProfileAccessContext> EnsureCanManageAccessAsync(
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);
}
