namespace Rafiq.Application.Features.Auth.DTOs;

public sealed record IdentityUserDto(Guid UserId, string Email, string PhoneNumber, string Role);
