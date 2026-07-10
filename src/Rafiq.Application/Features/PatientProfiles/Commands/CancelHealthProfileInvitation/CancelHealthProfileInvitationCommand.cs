using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.CancelHealthProfileInvitation;

public sealed record CancelHealthProfileInvitationCommand(Guid InvitationId)
    : IRequest<ApiResponse<HealthProfileInvitationDto>>;
