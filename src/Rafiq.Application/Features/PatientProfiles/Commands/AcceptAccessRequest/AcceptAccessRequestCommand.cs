using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.AcceptAccessRequest;

public sealed record AcceptAccessRequestCommand(Guid RequestId)
    : IRequest<ApiResponse<HealthProfileInvitationDto>>;
