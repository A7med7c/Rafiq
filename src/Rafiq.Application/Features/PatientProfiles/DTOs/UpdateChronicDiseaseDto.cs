using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record UpdateChronicDiseaseDto(
    Guid? Id,
    string Name,
    DateOnly? DiagnosedAt,
    DiseaseStatus Status,
    string? Notes);