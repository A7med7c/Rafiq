using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.DeleteChronicDisease;

public sealed record DeleteChronicDiseaseCommand(
    Guid PatientProfileId,
    Guid DiseaseId
) : IRequest<ApiResponse<object>>;
