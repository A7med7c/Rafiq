namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record AccessibleHealthProfileDto(
    Guid UserHealthProfileId,
    Guid? UserId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodType,
    decimal? Height,
    decimal? Weight,
    string? ProfileImageUrl,
    string AccessRole,
    string? Relationship,
    string AccessOrigin,
    string ProfileType,
    bool IsSelf
);
