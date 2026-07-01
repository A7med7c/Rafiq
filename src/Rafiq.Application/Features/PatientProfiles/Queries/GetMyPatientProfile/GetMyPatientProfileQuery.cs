using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetMyPatientProfile;

public sealed record GetMyPatientProfileQuery : IRequest<ApiResponse<PatientProfileDto>>;
