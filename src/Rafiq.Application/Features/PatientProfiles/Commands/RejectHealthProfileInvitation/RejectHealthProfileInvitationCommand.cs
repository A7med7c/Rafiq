using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RejectHealthProfileInvitation;

public sealed record RejectHealthProfileInvitationCommand(Guid InvitationId)
    : IRequest<ApiResponse<HealthProfileInvitationDto>>;
