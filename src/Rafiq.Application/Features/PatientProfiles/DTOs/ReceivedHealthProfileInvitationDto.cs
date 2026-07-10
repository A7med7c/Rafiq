namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record ReceivedHealthProfileInvitationDto(
    Guid Id,
    Guid UserHealthProfileId,
    string ProfileFirstName,
    string ProfileLastName,
    string ProfileType,
    string Role,
    string Status,
    Guid? InvitedByUserId,
    string? InviterFirstName,
    string? InviterLastName,
    string? InviterEmail,
    DateTime CreatedAt
);
