namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record PatientProfileDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodType,
    decimal? Height,
    decimal? Weight,
    string? ProfileImageUrl,
    IReadOnlyList<AllergyDto> Allergies,
    IReadOnlyList<ChronicDiseaseDto> ChronicDiseases,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);