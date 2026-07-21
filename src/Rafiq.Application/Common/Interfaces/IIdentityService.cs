using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<RegisterResponseDto> CreateUserAsync(string firstName, string lastName, string email, string phoneNumber, string password,
            string? profileImageUrl = null, CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<IdentityUserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task ConfirmPhoneNumberAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IdentityUserDto> LoginWithGoogleAsync(string IdToken, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<AccountDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountDto> UpdateAccountAsync(Guid userId, string firstName, string lastName, string phoneNumber, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);
    Task<AccountDto> CancelEmailUpdateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
