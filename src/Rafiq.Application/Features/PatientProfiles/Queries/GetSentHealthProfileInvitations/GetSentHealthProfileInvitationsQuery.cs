using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Queries.GetSentHealthProfileInvitations;

public sealed record GetSentHealthProfileInvitationsQuery
    : IRequest<ApiResponse<IReadOnlyList<SentHealthProfileInvitationDto>>>;
