using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(
    Guid ProfileId,
    Guid AccessId,
    AccessRole Role
) : IRequest<ApiResponse<HealthProfileInvitationDto>>;
