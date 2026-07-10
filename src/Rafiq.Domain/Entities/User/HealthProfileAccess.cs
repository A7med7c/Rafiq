using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Domain.Entities.User;

public class HealthProfileAccess : BaseEntity
{
    private HealthProfileAccess() { } // EF Core

    private HealthProfileAccess(
        Guid userHealthProfileId,
        Guid granteeUserId,
        AccessRole role,
        AccessStatus status,
        Guid? invitedByUserId,
        AccessOrigin origin)
    {
        UserHealthProfileId = userHealthProfileId;
        GranteeUserId = granteeUserId;
        Role = role;
        Status = status;
        InvitedByUserId = invitedByUserId;
        Origin = origin;
        StatusChangedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Grants a registered user Owner access to their own Self Profile. No invitation required.
    /// </summary>
    public static HealthProfileAccess CreateSelfOwner(
        Guid userHealthProfileId,
        Guid granteeUserId)
    {
        return new HealthProfileAccess(
            userHealthProfileId,
            granteeUserId,
            AccessRole.Owner,
            AccessStatus.Active,
            invitedByUserId: null,
            AccessOrigin.Direct);
    }

    /// <summary>
    /// Grants the creator of a Managed Profile Owner access. No invitation required.
    /// </summary>
    public static HealthProfileAccess CreateManagedProfileOwner(
        Guid userHealthProfileId,
        Guid granteeUserId)
    {
        return new HealthProfileAccess(
            userHealthProfileId,
            granteeUserId,
            AccessRole.Owner,
            AccessStatus.Active,
            invitedByUserId: null,
            AccessOrigin.Direct);
    }

    /// <summary>
    /// Creates a pending invitation for additional access (Owner, Manager, or Viewer).
    /// The Grantee is the decision maker: they accept or reject.
    /// </summary>
    public static HealthProfileAccess CreateInvitation(
        Guid userHealthProfileId,
        Guid granteeUserId,
        AccessRole role,
        Guid invitedByUserId)
    {
        return new HealthProfileAccess(
            userHealthProfileId,
            granteeUserId,
            role,
            AccessStatus.Pending,
            invitedByUserId,
            AccessOrigin.GrantInvitation);
    }

    /// <summary>
    /// Creates a pending request for access to another registered user's Self Profile.
    /// The requester becomes the Grantee, but the profile owner is the decision maker
    /// who accepts or rejects - the reverse of <see cref="CreateInvitation"/>.
    /// </summary>
    public static HealthProfileAccess CreateAccessRequest(
        Guid userHealthProfileId,
        Guid requesterUserId,
        AccessRole role)
    {
        return new HealthProfileAccess(
            userHealthProfileId,
            requesterUserId,
            role,
            AccessStatus.Pending,
            invitedByUserId: requesterUserId,
            AccessOrigin.AccessRequest);
    }

    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    /// <summary>
    /// The registered ApplicationUser who has or is requesting access.
    /// </summary>
    public Guid GranteeUserId { get; private set; }

    public AccessRole Role { get; private set; }

    public AccessStatus Status { get; private set; }

    /// <summary>
    /// How this record originated. Determines who is the decision maker while Pending:
    /// for GrantInvitation it's the Grantee, for AccessRequest it's the target profile's owner.
    /// </summary>
    public AccessOrigin Origin { get; private set; }

    /// <summary>
    /// Historical record of who originally sent the invitation or made the request.
    /// Does not determine who can manage this access.
    /// </summary>
    public Guid? InvitedByUserId { get; private set; }

    public DateTime StatusChangedAt { get; private set; }

    public void Accept()
    {
        EnsureStatus(AccessStatus.Pending, "accepted");
        SetStatus(AccessStatus.Active);
    }

    public void Reject()
    {
        EnsureStatus(AccessStatus.Pending, "rejected");
        SetStatus(AccessStatus.Rejected);
    }

    public void Cancel()
    {
        EnsureStatus(AccessStatus.Pending, "cancelled");
        SetStatus(AccessStatus.Cancelled);
    }

    public void Revoke()
    {
        EnsureStatus(AccessStatus.Active, "revoked");
        SetStatus(AccessStatus.Revoked);
    }

    public void Leave()
    {
        EnsureStatus(AccessStatus.Active, "left");
        SetStatus(AccessStatus.Left);
    }

    public void ChangeRole(AccessRole newRole)
    {
        if (Status != AccessStatus.Active)
            throw new BadRequestException("Only active access can have its role changed.");

        Role = newRole;
        MarkUpdated();
    }

    private void EnsureStatus(AccessStatus expected, string action)
    {
        if (Status != expected)
            throw new BadRequestException($"Access cannot be {action} unless it is {expected}.");
    }

    private void SetStatus(AccessStatus status)
    {
        Status = status;
        StatusChangedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
