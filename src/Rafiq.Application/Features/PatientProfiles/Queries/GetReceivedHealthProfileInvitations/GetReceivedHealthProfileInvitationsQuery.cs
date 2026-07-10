using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetReceivedHealthProfileInvitations;

public sealed record GetReceivedHealthProfileInvitationsQuery
    : IRequest<ApiResponse<IReadOnlyList<ReceivedHealthProfileInvitationDto>>>;
