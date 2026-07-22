using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.CreateAllergy;

public sealed record CreateAllergyCommand(
    Guid PatientProfileId,
    string Name,
    AllergySeverity Severity
) : IRequest<ApiResponse<AllergyDto>>;
