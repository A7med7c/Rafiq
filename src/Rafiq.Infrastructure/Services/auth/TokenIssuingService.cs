using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.auth
{
    public class TokenIssuingService(ITokenService tokenService, ITokenHasher tokenHasher
        , IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork) : ITokenIssuingService
    {
        public async Task<AuthResponseDto> IssueTokensAsync(IdentityUserDto user, CancellationToken cancellationToken)
        {
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

            var refreshTokenEntity = new Domain.Entities.User.RefreshToken(
                refreshTokenHash,
                accessTokenJti,
                user.UserId,
                refreshTokenExpiresAt);

            await refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto(
                    accessToken,
                    refreshToken,
                    accessTokenExpiresAt,
                    refreshTokenExpiresAt);
        }
    }
}
