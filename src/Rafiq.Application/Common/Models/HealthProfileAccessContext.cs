using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Models;

/// <summary>
/// The current authenticated user's access to a specific health profile.
/// AccessOrigin is deliberately not considered here - it describes how access
/// was created, not what the user is allowed to do with it.
/// </summary>
public sealed record HealthProfileAccessContext(
    Guid UserHealthProfileId,
    Guid CurrentUserId,
    Guid AccessId,
    AccessRole Role,
    AccessStatus Status)
{
    public bool CanRead => Status == AccessStatus.Active;

    public bool CanWrite => Status == AccessStatus.Active && Role is AccessRole.Owner or AccessRole.Manager;

    public bool CanManageAccess => Status == AccessStatus.Active && Role == AccessRole.Owner;
}
