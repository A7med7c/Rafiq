using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.UpdateChronicDisease;

public sealed record UpdateChronicDiseaseCommand(
    Guid PatientProfileId,
    Guid DiseaseId,
    string Name,
    DateOnly? DiagnosedAt,
    DiseaseStatus Status
) : IRequest<ApiResponse<ChronicDiseaseDto>>;
