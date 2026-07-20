namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// The kind of medical record that was added to a profile. Used to build the
/// activity-notification text without leaking any of the record's contents.
/// </summary>
public enum MedicalRecordKind
{
    LabReport = 1,
    ImagingReport = 2,
    Prescription = 3
}

/// <summary>
/// Sends the profile-sharing lifecycle and profile-activity notifications on top of the
/// existing <see cref="INotificationService"/> (SignalR <c>ReceiveNotification</c> channel).
/// Every method is best-effort: an implementation must never throw, so a failed notification
/// can never break the command that triggered it. Recipient resolution (role filtering,
/// de-duplication, and skipping deleted/inactive users) lives entirely inside the implementation
/// so command handlers and reminder jobs stay free of duplicated routing logic.
/// </summary>
public interface IProfileNotificationService
{
    /// <summary>Invitation Sent — notify the invited user (grantee).</summary>
    Task NotifyInvitationSentAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Invitation Accepted — notify the user who originally sent the invitation.</summary>
    Task NotifyInvitationAcceptedAsync(
        Guid actorUserId,
        Guid? inviterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Invitation Rejected — notify the user who originally sent the invitation.</summary>
    Task NotifyInvitationRejectedAsync(
        Guid actorUserId,
        Guid? inviterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Access Request Sent — notify every active Owner of the target profile.</summary>
    Task NotifyAccessRequestSentAsync(
        Guid userHealthProfileId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Access Request Approved — notify the requester.</summary>
    Task NotifyAccessRequestApprovedAsync(
        Guid requesterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Access Request Rejected — notify the requester.</summary>
    Task NotifyAccessRequestRejectedAsync(
        Guid requesterUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Role Changed — notify the affected member.</summary>
    Task NotifyRoleChangedAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Access Removed — notify the affected member.</summary>
    Task NotifyAccessRemovedAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Profile Activity — notify everyone who may view the profile (Owner, Manager, Viewer)
    /// that a new medical record was added, excluding the user who added it.
    /// </summary>
    Task NotifyMedicalRecordAddedAsync(
        Guid userHealthProfileId,
        Guid actorUserId,
        MedicalRecordKind kind,
        CancellationToken cancellationToken = default);
}
