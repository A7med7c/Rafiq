using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Services.auth;

public sealed class IdentityService(
    UserManager<ApplicationUser> _userManager,
    SignInManager<ApplicationUser> _signInManager,
    RoleManager<IdentityRole<Guid>> _roleManager,
    IGoogleTokenValidator _googleTokenValidator) : IIdentityService
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => _userManager.Users.AnyAsync(x => x.Email == email, cancellationToken);

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => _userManager.Users.AnyAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<RegisterResponseDto> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(role, cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = email,
            PhoneNumber = phoneNumber,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
            throw new ValidationException(createResult.Errors.Select(x => x.Description));

        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
            throw new ValidationException(roleResult.Errors.Select(x => x.Description));

        return new RegisterResponseDto(
            user.Id,
            user.Email!,
            user.PhoneNumber!,
            role,
            user.PhoneNumberConfirmed);
    }
    public async Task<IdentityUserDto?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return null;


        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

        return new IdentityUserDto(
            user.Id,
            user.Email!,
            user.PhoneNumber!,
            role,
            user.PhoneNumberConfirmed);
    }

    public async Task<IdentityUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        return new IdentityUserDto(user.Id, user.Email!, user.PhoneNumber!, role, user.PhoneNumberConfirmed);
    }

    public async Task<IdentityUserDto?> GetByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber,
            cancellationToken);
        if (user is null || !user.IsActive)
            return null;


        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        return new IdentityUserDto(user.Id, user.Email!, user.PhoneNumber!, role, user.PhoneNumberConfirmed);
    }

    public async Task ConfirmPhoneNumberAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User", user.Id);

        user.PhoneNumberConfirmed = true;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(x => x.Description));
    }
    public async Task<IdentityUserDto> LoginWithGoogleAsync(string IdToken, CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleTokenValidator.ValidateAsync(IdToken, cancellationToken);
        if (!googleUser.EmailVerified)
            throw new AuthenticationException("Google email is not verified.");

        var user = await _userManager.FindByEmailAsync(googleUser.Email);
        if (user is null)
        {
            await EnsureRoleExistsAsync("Patient", cancellationToken);
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = googleUser.Email,
                UserName = googleUser.Email,
                FirstName = googleUser.FirstName,
                LastName = googleUser.LastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                IsActive = true
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new ValidationException(createResult.Errors.Select(x => x.Description));

            var roleResult = await _userManager.AddToRoleAsync(user, "Patient");
            if (!roleResult.Succeeded)
                throw new ValidationException(createResult.Errors.Select(x => x.Description));
        }
        if (!user.IsActive)
            throw new AuthenticationException("Your account has been disabled.");

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

        return new IdentityUserDto(user.Id, user.Email!, user.PhoneNumber!, role, user.PhoneNumberConfirmed);

    }

    private async Task EnsureRoleExistsAsync(string role, CancellationToken cancellationToken)
    {
        if (await _roleManager.RoleExistsAsync(role))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(x => x.Description));
        }
    }
}