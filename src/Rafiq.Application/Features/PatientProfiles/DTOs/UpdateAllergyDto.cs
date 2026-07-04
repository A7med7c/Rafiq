using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record UpdateAllergyDto(
    Guid? Id,
    string Name,
    AllergySeverity Severity,
    string? Notes);