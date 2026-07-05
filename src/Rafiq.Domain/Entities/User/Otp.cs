using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.User
{
    public class Otp : BaseEntity
    {
        public Guid UserId { get; private set; }

        public string CodeHash { get; private set; } = null!;

        public OtpPurpose Purpose { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public bool IsUsed { get; private set; }

        public DateTime? UsedAt { get; private set; }

        private Otp() { }

        public Otp(
            Guid userId,
            string codeHash,
            OtpPurpose purpose,
            DateTime expiresAt)
        {
            UserId = userId;
            CodeHash = codeHash;
            Purpose = purpose;
            ExpiresAt = expiresAt;
        }

        public void MarkAsUsed()
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
        }

        public bool IsExpired()
            => DateTime.UtcNow > ExpiresAt;
    }
}
