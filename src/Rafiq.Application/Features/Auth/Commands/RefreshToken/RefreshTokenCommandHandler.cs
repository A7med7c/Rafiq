using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityService identityService,
    ITokenService tokenService,
    ITokenHasher tokenHasher,
    IEmergencyContactRepository emergencyContactRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
{
    public async Task<ApiResponse<AuthResponseDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenHasher.Hash(request.RefreshToken);

        var existingToken = await refreshTokenRepository
            .GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        // Refresh token reuse detected
        if (existingToken.IsRevoked)
        {
            await refreshTokenRepository.RevokeAllByUserIdAsync(
                existingToken.UserId,
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(
                "Refresh token reuse detected.");
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedException(
                "Refresh token has expired.");
        }

        var user = await identityService.GetByIdAsync(
            existingToken.UserId,
            cancellationToken)
            ?? throw new UnauthorizedException("User no longer exists.");

        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        var accessTokenJti = Guid.NewGuid().ToString();

        var accessToken = tokenService.GenerateAccessToken(
            user.UserId,
            user.Email,
            user.Role,
            accessTokenJti,
            accessTokenExpiresAt);

        var refreshToken = tokenService.GenerateRefreshToken();

        var refreshTokenHash = tokenHasher.Hash(refreshToken);

        var newRefreshToken = new Domain.Entities.User.RefreshToken(
            refreshTokenHash,
            accessTokenJti,
            existingToken.UserId,
            refreshTokenExpiresAt,
            request.DeviceInfo,
            request.IpAddress);

        existingToken.Revoke(
            request.IpAddress,
            refreshTokenHash);

        await refreshTokenRepository.AddAsync(
            newRefreshToken,
            cancellationToken);

        var contacts = await emergencyContactRepository.GetAllByUserIdAsync(existingToken.UserId, cancellationToken);
        var hasEmergencyContacts = contacts.Any();

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            new AuthResponseDto(
                accessToken,
                refreshToken,
                accessTokenExpiresAt,
                refreshTokenExpiresAt,
                hasEmergencyContacts),
            "Token refreshed successfully.");
    }
}