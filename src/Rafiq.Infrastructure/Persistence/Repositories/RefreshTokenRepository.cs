using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly RafiqDbContext _dbContext;

    public RefreshTokenRepository(RafiqDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId &&
                        !x.IsRevoked &&
                        x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .AddAsync(refreshToken, cancellationToken)
            .AsTask();
    }

    public void Update(RefreshToken refreshToken)
    {
        _dbContext.RefreshTokens.Update(refreshToken);
    }

    public async Task RevokeAllByUserIdAsync(
        Guid userId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await GetActiveByUserIdAsync(
            userId,
            cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(revokedByIp);
        }
    }
}