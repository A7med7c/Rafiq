using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Repositories
{
    public interface IOtpRepository
    {
        Task AddAsync(Otp otp, CancellationToken cancellationToken);

        Task<Otp?> GetLatestAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken);

        void Remove(Otp otp);

        void Update(Otp otp);
        Task RemoveOldOtpsAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken = default);
    }
}
