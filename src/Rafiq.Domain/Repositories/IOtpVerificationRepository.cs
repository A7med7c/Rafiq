using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Repositories
{
    public interface IOtpVerificationRepository
    {
        Task AddAsync(
            OtpVerification verification,
            CancellationToken cancellationToken);

        void Update(OtpVerification verification);

        Task<OtpVerification?> GetLatestAsync(
     Guid userId,
     OtpPurpose purpose,
     CancellationToken cancellationToken);

        Task DeleteByUserIdAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken);

        Task<bool> ExistsActiveCodeAsync(Guid userId,
     OtpPurpose purpose,
     CancellationToken cancellationToken);
    }
}
