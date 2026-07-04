using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<RegisterResponseDto> CreateUserAsync(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string password,
            string role,
            CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task ConfirmPhoneNumberAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IdentityUserDto> LoginWithGoogleAsync(string IdToken, CancellationToken cancellationToken = default);
}
