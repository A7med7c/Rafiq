using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.LeaveHealthProfile;

public sealed record LeaveHealthProfileCommand(Guid ProfileId)
    : IRequest<ApiResponse<HealthProfileInvitationDto>>;
