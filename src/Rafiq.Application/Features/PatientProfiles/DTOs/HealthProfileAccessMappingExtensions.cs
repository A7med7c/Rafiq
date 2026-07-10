using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Application.Features.PatientProfiles.DTOs;

internal static class HealthProfileAccessMappingExtensions
{
    public static HealthProfileInvitationDto ToDto(this HealthProfileAccess access)
        => new(
            access.Id,
            access.UserHealthProfileId,
            access.GranteeUserId,
            access.Role.ToString(),
            access.Status.ToString(),
            access.Origin.ToString(),
            access.InvitedByUserId,
            access.StatusChangedAt,
            access.CreatedAt);

    public static ReceivedHealthProfileInvitationDto ToReceivedInvitationDto(
        this HealthProfileAccess access,
        AccountDto? inviter)
        => new(
            access.Id,
            access.UserHealthProfileId,
            access.UserHealthProfile.FirstName,
            access.UserHealthProfile.LastName,
            access.UserHealthProfile.UserId is not null ? "Self" : "Managed",
            access.Role.ToString(),
            access.Status.ToString(),
            access.InvitedByUserId,
            inviter?.FirstName,
            inviter?.LastName,
            inviter?.Email,
            access.CreatedAt);

    public static ReceivedAccessRequestDto ToReceivedAccessRequestDto(
        this HealthProfileAccess access,
        AccountDto requester)
        => new(
            access.Id,
            access.UserHealthProfileId,
            access.GranteeUserId,
            requester.FirstName,
            requester.LastName,
            requester.Email,
            access.Role.ToString(),
            access.Status.ToString(),
            access.CreatedAt);

    public static AccessibleHealthProfileDto ToAccessibleProfileDto(
        this HealthProfileAccess access,
        Guid currentUserId)
    {
        var profile = access.UserHealthProfile;
        var isSelf = profile.UserId == currentUserId;
        var profileType = isSelf ? "Self" : profile.UserId is null ? "Managed" : "Shared";

        return new AccessibleHealthProfileDto(
            profile.Id,
            profile.UserId,
            profile.FirstName,
            profile.LastName,
            profile.DateOfBirth,
            profile.Gender.ToString(),
            profile.BloodType?.ToString(),
            profile.Height,
            profile.Weight,
            access.Role.ToString(),
            access.Origin.ToString(),
            profileType,
            isSelf);
    }

    public static ProfileMemberDto ToProfileMemberDto(
        this HealthProfileAccess access,
        AccountDto grantee,
        Guid currentUserId)
        => new(
            access.Id,
            access.GranteeUserId,
            grantee.FirstName,
            grantee.LastName,
            grantee.Email,
            access.Role.ToString(),
            access.Status.ToString(),
            access.Origin.ToString(),
            access.GranteeUserId == currentUserId,
            access.CreatedAt);
}
