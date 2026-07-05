using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.ExternalLogin
{
    public class GoogleLoginCommandHandler(IIdentityService identityService,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<GoogleLoginCommand, ApiResponse<AuthResponseDto>>
    {
        public async Task<ApiResponse<AuthResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var user = await identityService.LoginWithGoogleAsync(request.IdToken, cancellationToken);

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

            await refreshTokenRepository.AddAsync(
                new Domain.Entities.User.RefreshToken(
                    refreshTokenHash,
                    accessTokenJti,
                    user.UserId,
                    refreshTokenExpiresAt),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<AuthResponseDto>.SuccessResponse(
                new AuthResponseDto(
                    accessToken,
                    refreshToken,
                    accessTokenExpiresAt,
                    refreshTokenExpiresAt),
                "Google login successful.");
        }
    }
}
