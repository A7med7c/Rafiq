using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record UpdateAllergyDto(
    Guid? Id,
    string Name,
    string? Reaction,
    AllergySeverity Severity,
    string? Notes);