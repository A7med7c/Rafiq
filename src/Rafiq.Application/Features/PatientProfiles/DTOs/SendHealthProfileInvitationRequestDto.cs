using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.DTOs;

public sealed record SendHealthProfileInvitationRequestDto(
    string Email,
    AccessRole Role
);
