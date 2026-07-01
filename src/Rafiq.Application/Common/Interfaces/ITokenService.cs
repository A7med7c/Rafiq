namespace Rafiq.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string role, string jti, DateTime expiresAt);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
