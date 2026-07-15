using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;

namespace Rafiq.Application.Features.PatientProfiles.Commands.UpdateProfileImage;

public sealed record UpdateProfileImageCommand(
    Guid ProfileId,
    IFormFile? ProfileImage,
    bool RemoveImage
) : IRequest<ApiResponse<PatientProfileDto>>
{
    public Guid? EntityId => ProfileId;
}
