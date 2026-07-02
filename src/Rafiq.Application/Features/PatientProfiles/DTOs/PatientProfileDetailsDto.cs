namespace Rafiq.Application.Features.PatientProfiles.DTOs
{
    public sealed record PatientProfileDetailsDto(
        Guid Id,
        string FullName,
        string Email,
        PatientProfileDto Profile
    );
}