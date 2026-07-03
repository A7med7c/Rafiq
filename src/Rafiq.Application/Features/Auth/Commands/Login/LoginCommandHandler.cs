using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    public async Task<ApiResponse<AuthResponseDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken)
            ?? throw new AuthenticationException("Invalid email or password.");

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

        var refreshTokenEntity = new Domain.Entities.RefreshToken(
            refreshTokenHash,
            accessTokenJti,
            user.UserId,
            refreshTokenExpiresAt,
            request.DeviceInfo,
            request.IpAddress);

        await refreshTokenRepository.AddAsync(
            refreshTokenEntity,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            new AuthResponseDto(
                accessToken,
                refreshToken,
                accessTokenExpiresAt,
                refreshTokenExpiresAt),
            "Login successful.");
    }
}