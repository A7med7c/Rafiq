namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record PatientProfileDto(
    Guid Id,
    Guid UserId,
    DateOnly DateOfBirth,
    string Gender,
    string BloodType,
    decimal Height,
    decimal Weight,
    IReadOnlyList<AllergyDto> Allergies,
    IReadOnlyList<ChronicDiseaseDto> ChronicDiseases,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);