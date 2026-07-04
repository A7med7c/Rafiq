using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories
{
    public interface IPhoneVerificationRepository
    {
        Task AddAsync(
            PhoneVerification verification,
            CancellationToken cancellationToken);

        void Update(PhoneVerification verification);

        Task<PhoneVerification?> GetLatestAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task DeleteByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> ExistsActiveCodeAsync(
            Guid userId,
            CancellationToken cancellationToken);
    }
}
