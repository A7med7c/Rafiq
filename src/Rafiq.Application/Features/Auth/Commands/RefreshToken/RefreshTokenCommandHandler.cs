using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityService identityService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _identityService = identityService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (!existingToken.IsActive)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(existingToken.UserId, request.IpAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var jti = Guid.NewGuid().ToString();
        var user = await _identityService.GetByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");
        var accessToken = _tokenService.GenerateAccessToken(user.UserId, user.Email, user.Role, jti, accessTokenExpiresAt);
        var newRefreshTokenEntity = new Domain.Entities.RefreshToken(
            _tokenService.HashRefreshToken(newRefreshToken),
            jti,
            existingToken.UserId,
            refreshTokenExpiresAt,
            request.DeviceInfo,
            request.IpAddress);

        existingToken.Revoke(request.IpAddress, newRefreshTokenEntity.Token);
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            new AuthResponseDto(accessToken, newRefreshToken, accessTokenExpiresAt, refreshTokenExpiresAt),
            "Token refreshed successfully.");
    }
}
