namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record HealthProfileInvitationDto(
    Guid Id,
    Guid UserHealthProfileId,
    Guid GranteeUserId,
    string Role,
    string Status,
    string Origin,
    Guid? InvitedByUserId,
    DateTime StatusChangedAt,
    DateTime CreatedAt
);
