namespace Rafiq.Application.Features.Auth.DTOs
{
    public sealed record GoogleUserInfoDto(
        string GoogleId,
        string Email,
        string FirstName,
        string LastName,
        string PictureUrl,
        bool EmailVerified);
}