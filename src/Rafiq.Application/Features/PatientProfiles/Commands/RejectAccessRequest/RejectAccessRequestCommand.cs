using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RejectAccessRequest;

public sealed record RejectAccessRequestCommand(Guid RequestId)
    : IRequest<ApiResponse<HealthProfileInvitationDto>>;
