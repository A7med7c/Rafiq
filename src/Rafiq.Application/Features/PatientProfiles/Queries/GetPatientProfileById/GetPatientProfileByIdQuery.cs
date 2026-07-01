using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;

public sealed record GetPatientProfileByIdQuery(Guid PatientProfileId)
    : IRequest<ApiResponse<PatientProfileDto>>, IPatientOwnedRequest;
