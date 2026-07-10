using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RequestHealthProfileAccess;

public sealed record RequestHealthProfileAccessCommand(
    string Email,
    AccessRole Role
) : IRequest<ApiResponse<HealthProfileInvitationDto>>;
