using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Infrastructure.Persistence.Identity;

namespace Rafiq.Infrastructure.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => _userManager.Users.AnyAsync(x => x.Email == email, cancellationToken);

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => _userManager.Users.AnyAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<RegisterResponseDto> RegisterAsync(
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
            PhoneNumberConfirmed = true,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException(createResult.Errors.Select(x => x.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new ValidationException(roleResult.Errors.Select(x => x.Description));
        }

        return new RegisterResponseDto(user.Id, user.Email!, user.PhoneNumber!, role);
    }

    public async Task<IdentityUserDto?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return null;
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        return new IdentityUserDto(user.Id, user.Email!, user.PhoneNumber!, role);
    }

    public async Task<IdentityUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        return new IdentityUserDto(user.Id, user.Email!, user.PhoneNumber!, role);
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
