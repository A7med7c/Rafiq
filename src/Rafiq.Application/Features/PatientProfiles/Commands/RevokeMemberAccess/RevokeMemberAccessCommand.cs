using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.RevokeMemberAccess;

public sealed record RevokeMemberAccessCommand(Guid ProfileId, Guid AccessId)
    : IRequest<ApiResponse<HealthProfileInvitationDto>>;
