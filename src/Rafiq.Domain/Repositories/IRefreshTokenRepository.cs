using Rafiq.Domain.Entities.User;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    void Update(RefreshToken refreshToken);

    Task RevokeAllByUserIdAsync(
        Guid userId,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);
}