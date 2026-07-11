using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetReceivedAccessRequests;

public sealed record GetReceivedAccessRequestsQuery
    : IRequest<ApiResponse<IReadOnlyList<ReceivedAccessRequestDto>>>;
