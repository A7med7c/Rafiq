namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record SentHealthProfileInvitationDto(
    Guid Id,
    Guid UserHealthProfileId,
    string ProfileFirstName,
    string ProfileLastName,
    string GranteeEmail,
    string? GranteeFirstName,
    string? GranteeLastName,
    string Role,
    string Status,
    DateTime CreatedAt
);
