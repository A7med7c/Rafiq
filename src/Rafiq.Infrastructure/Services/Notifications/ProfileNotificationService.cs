using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Services.Notifications;

/// <summary>
/// Reuses the existing <see cref="INotificationService"/> SignalR pipeline (the generic
/// <c>ReceiveNotification</c> title/message event the Angular client already renders) to deliver
/// profile-sharing and profile-activity notifications. All routing, de-duplication, and
/// deleted/inactive-user filtering lives here; every public method is best-effort and never throws.
/// </summary>
public sealed class ProfileNotificationService : IProfileNotificationService
{
    // Owner and Manager are the profile's decision makers; Viewer can additionally read records.
    private static readonly AccessRole[] OwnerRoles = [AccessRole.Owner];
    private static readonly AccessRole[] ReaderRoles = [AccessRole.Owner, AccessRole.Manager, AccessRole.Viewer];

    private readonly INotificationService _notificationService;
    private readonly IHealthProfileAccessRepository _accessRepository;
    private readonly IPatientProfileRepository _profileRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileNotificationService> _logger;

    public ProfileNotificationService(
        INotificationService notificationService,
        IHealthProfileAccessRepository accessRepository,
        IPatientProfileRepository profileRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<ProfileNotificationService> logger)
    {
        _notificationService = notificationService;
        _accessRepository = accessRepository;
        _profileRepository = profileRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public Task NotifyInvitationSentAsync(
        Guid userHealthProfileId,
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(async () =>
        {
            var profileName = await ResolveProfileNameAsync(userHealthProfileId, cancellationToken);
            await SendAsync(
                granteeUserId,
                "Profile Invitation",
                $"You have been invited to access {profileName}.",
                cancellationToken);
        }, nameof(NotifyInvitationSentAsync));

    public Task NotifyInvitationAcceptedAsync(
        Guid actorUserId,
        Guid? inviterUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(async () =>
        {
            if (inviterUserId is null)
                return;

            var name = await ResolveUserNameAsync(actorUserId);
            await SendAsync(
                inviterUserId.Value,
                "Invitation Accepted",
                $"{name} accepted your invitation.",
                cancellationToken);
        }, nameof(NotifyInvitationAcceptedAsync));

    public Task NotifyInvitationRejectedAsync(
        Guid actorUserId,
        Guid? inviterUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(async () =>
        {
            if (inviterUserId is null)
                return;

            var name = await ResolveUserNameAsync(actorUserId);
            await SendAsync(
                inviterUserId.Value,
                "Invitation Rejected",
                $"{name} rejected your invitation.",
                cancellationToken);
        }, nameof(NotifyInvitationRejectedAsync));

    public Task NotifyAccessRequestSentAsync(
        Guid userHealthProfileId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(async () =>
        {
            var name = await ResolveUserNameAsync(requesterUserId);
            var owners = await _accessRepository.GetActiveGranteeUserIdsByRolesAsync(
                userHealthProfileId, OwnerRoles, cancellationToken);

            await SendManyAsync(
                owners,
                excludeUserId: requesterUserId,
                title: "Access Request",
                message: $"{name} requested access to your profile.",
                cancellationToken);
        }, nameof(NotifyAccessRequestSentAsync));

    public Task NotifyAccessRequestApprovedAsync(
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(() => SendAsync(
            requesterUserId,
            "Access Request Approved",
            "Your access request has been approved.",
            cancellationToken), nameof(NotifyAccessRequestApprovedAsync));

    public Task NotifyAccessRequestRejectedAsync(
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(() => SendAsync(
            requesterUserId,
            "Access Request Rejected",
            "Your access request has been rejected.",
            cancellationToken), nameof(NotifyAccessRequestRejectedAsync));

    public Task NotifyRoleChangedAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(() => SendAsync(
            granteeUserId,
            "Access Updated",
            "Your access role has been updated.",
            cancellationToken), nameof(NotifyRoleChangedAsync));

    public Task NotifyAccessRemovedAsync(
        Guid granteeUserId,
        CancellationToken cancellationToken = default)
        => SafeAsync(() => SendAsync(
            granteeUserId,
            "Access Removed",
            "Your access to this profile has been removed.",
            cancellationToken), nameof(NotifyAccessRemovedAsync));

    public Task NotifyMedicalRecordAddedAsync(
        Guid userHealthProfileId,
        Guid actorUserId,
        MedicalRecordKind kind,
        CancellationToken cancellationToken = default)
        => SafeAsync(async () =>
        {
            var (title, label) = Describe(kind);
            var profileName = await ResolveProfileNameAsync(userHealthProfileId, cancellationToken);
            var recipients = await _accessRepository.GetActiveGranteeUserIdsByRolesAsync(
                userHealthProfileId, ReaderRoles, cancellationToken);

            await SendManyAsync(
                recipients,
                excludeUserId: actorUserId,
                title: title,
                message: $"A new {label} was added to {profileName}.",
                cancellationToken);
        }, nameof(NotifyMedicalRecordAddedAsync));

    private static (string Title, string Label) Describe(MedicalRecordKind kind) => kind switch
    {
        MedicalRecordKind.LabReport => ("New Lab Report", "lab report"),
        MedicalRecordKind.ImagingReport => ("New Imaging Report", "imaging report"),
        MedicalRecordKind.Prescription => ("New Prescription", "prescription"),
        _ => ("New Medical Record", "medical record")
    };

    /// <summary>Sends to each recipient once, skipping the actor. The id list is already distinct.</summary>
    private async Task SendManyAsync(
        IReadOnlyList<Guid> userIds,
        Guid excludeUserId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        foreach (var userId in userIds)
        {
            if (userId == excludeUserId)
                continue;

            await SendAsync(userId, title, message, cancellationToken);
        }
    }

    /// <summary>
    /// Delivers a single notification, first confirming the recipient is a live account.
    /// Soft-deleted users are excluded by the ApplicationUser query filter (FindByIdAsync
    /// returns null); deactivated accounts are skipped explicitly. Never throws.
    /// </summary>
    private async Task SendAsync(Guid userId, string title, string message, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null || user.IsDeleted || !user.IsActive)
            {
                _logger.LogInformation(
                    "Skipping profile notification for user {UserId}: account missing, deleted, or inactive.",
                    userId);
                return;
            }

            await _notificationService.SendNotificationToUserAsync(
                userId.ToString(), title, message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Best-effort delivery: one recipient failing must not abort the others or the caller.
            _logger.LogError(ex, "Failed to send profile notification to user {UserId}.", userId);
        }
    }

    private async Task<string> ResolveUserNameAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return "Someone";

        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Someone" : name;
    }

    private async Task<string> ResolveProfileNameAsync(Guid userHealthProfileId, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByIdAsync(userHealthProfileId, cancellationToken);
        if (profile is null)
            return "the profile";

        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "the profile" : name;
    }

    private async Task SafeAsync(Func<Task> action, string context)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile notification '{Context}' failed.", context);
        }
    }
}
