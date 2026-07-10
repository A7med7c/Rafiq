namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record ProfileMemberDto(
    Guid AccessId,
    Guid GranteeUserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string Status,
    string Origin,
    bool IsCurrentUser,
    DateTime CreatedAt
);
