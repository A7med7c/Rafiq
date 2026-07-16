using Microsoft.AspNetCore.Http;

namespace Rafiq.Application.Features.PatientProfiles.DTOs
{
    public record UpdateProfileImageRequestDto(
    IFormFile? ProfileImage,
    bool RemoveImage);
}
