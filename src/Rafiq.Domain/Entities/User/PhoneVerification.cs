using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.User;

public sealed class OtpVerification : BaseEntity
{
    private OtpVerification() { }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = default!;

    public DateTime ExpiresAt { get; private set; }

    public bool IsUsed { get; private set; }

    public int Attempts { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime LastSentAt { get; private set; }

    public int ResendCount { get; private set; }
    public OtpPurpose Purpose { get; private set; }
    public static OtpVerification Create(
    Guid userId,
    string codeHash,
    OtpPurpose purpose,
    DateTime expiresAt)
    {
        return new OtpVerification
        {
            UserId = userId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            LastSentAt = DateTime.UtcNow,
            Attempts = 0,
            ResendCount = 0,
            IsUsed = false,
            Purpose = purpose
        };
    }

    public bool IsExpired()
        => DateTime.UtcNow > ExpiresAt;

    public bool CanVerify()
        => !IsUsed && !IsExpired() && Attempts < 5;

    public void IncrementAttempts()
        => Attempts++;

    public void MarkAsUsed()
        => IsUsed = true;

    public void IncrementResend()
    {
        ResendCount++;
        LastSentAt = DateTime.UtcNow;
    }

    public bool CanResend()
    {
        return DateTime.UtcNow >= LastSentAt.AddMinutes(1);
    }
}