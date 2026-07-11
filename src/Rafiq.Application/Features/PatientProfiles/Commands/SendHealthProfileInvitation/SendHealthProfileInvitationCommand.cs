using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.SendHealthProfileInvitation;

public sealed record SendHealthProfileInvitationCommand(
    Guid UserHealthProfileId,
    string Email,
    AccessRole Role
) : IRequest<ApiResponse<HealthProfileInvitationDto>>;
