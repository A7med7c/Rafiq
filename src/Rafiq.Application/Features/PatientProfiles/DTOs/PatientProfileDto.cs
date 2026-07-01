namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record PatientProfileDto(
    Guid Id,
    Guid? UserId,
    string FullName,
    DateTime DateOfBirth,
    string Gender,
    string? BloodType,
    string? Allergies,
    string? ChronicConditions,
    string EmergencyContactName,
    string EmergencyContactPhone,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
