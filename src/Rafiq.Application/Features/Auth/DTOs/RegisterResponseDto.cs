namespace Rafiq.Application.Features.Auth.DTOs;

public sealed record RegisterResponseDto(Guid UserId, string Email, string PhoneNumber, string Role);
