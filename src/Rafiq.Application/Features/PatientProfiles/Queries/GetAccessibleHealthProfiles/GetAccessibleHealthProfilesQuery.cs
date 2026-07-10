using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetAccessibleHealthProfiles;

public sealed record GetAccessibleHealthProfilesQuery
    : IRequest<ApiResponse<IReadOnlyList<AccessibleHealthProfileDto>>>;
