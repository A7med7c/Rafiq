using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Repositories;
using AuthenticationException = Rafiq.Domain.Exceptions.AuthenticationException;

namespace Rafiq.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IIdentityService identityService,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var jti = Guid.NewGuid().ToString();
        var accessToken = _tokenService.GenerateAccessToken(user.UserId, user.Email, user.Role, jti, accessTokenExpiresAt);
        var tokenEntity = new Domain.Entities.RefreshToken(
            _tokenService.HashRefreshToken(refreshToken),
            jti,
            user.UserId,
            refreshTokenExpiresAt,
            request.DeviceInfo,
            request.IpAddress);

        await _refreshTokenRepository.AddAsync(tokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            new AuthResponseDto(accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt),
            "Login successful.");
    }
}
