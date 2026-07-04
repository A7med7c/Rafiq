using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.User;

public sealed class RefreshToken : BaseEntity
{
    private RefreshToken() { } // EF Core

    public RefreshToken(
        string tokenHash,
        string accessTokenJti,
        Guid userId,
        DateTime expiresAt,
        string? deviceInfo = null,
        string? ipAddress = null)
    {
        TokenHash = tokenHash;
        AccessTokenJti = accessTokenJti;
        UserId = userId;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public string AccessTokenJti { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public string? DeviceInfo { get; private set; }

    public string? IpAddress { get; private set; }

    public bool IsRevoked { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(
        string? revokedByIp = null,
        string? replacedByTokenHash = null)
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;

        MarkUpdated();
    }
}