using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public RefreshToken(
        string token,
        string jtiAccessToken,
        Guid userId,
        DateTime expiresAt,
        string? deviceInfo,
        string? ipAddress)
    {
        Token = token;
        JtiAccessToken = jtiAccessToken;
        UserId = userId;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
    }

    public string Token { get; private set; } = string.Empty;
    public string JtiAccessToken { get; private set; } = string.Empty;
    public string? DeviceInfo { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? IpAddress { get; private set; }
    public Guid UserId { get; private set; }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    public void Revoke(string? revokedByIp = null, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        MarkUpdated();
    }
}
