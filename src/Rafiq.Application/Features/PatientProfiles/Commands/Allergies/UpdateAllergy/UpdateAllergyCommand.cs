using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.UpdateAllergy;

public sealed record UpdateAllergyCommand(
    Guid PatientProfileId,
    Guid AllergyId,
    string Name,
    AllergySeverity Severity
) : IRequest<ApiResponse<AllergyDto>>;
