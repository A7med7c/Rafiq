namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record ReceivedAccessRequestDto(
    Guid Id,
    Guid UserHealthProfileId,
    Guid RequesterUserId,
    string RequesterFirstName,
    string RequesterLastName,
    string RequesterEmail,
    string Role,
    string Status,
    DateTime CreatedAt
);
