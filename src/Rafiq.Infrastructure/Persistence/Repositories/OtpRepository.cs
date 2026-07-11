using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories
{
    public class OtpRepository(RafiqDbContext dbContext) : IOtpRepository
    {
        public async Task AddAsync(Otp otp, CancellationToken cancellationToken)
            => await dbContext.Otps.AddAsync(otp, cancellationToken);

        public async Task<Otp?> GetLatestAsync(Guid userId, OtpPurpose purpose, CancellationToken cancellationToken = default)
        {
            return await dbContext.Otps
                .Where(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public void Remove(Otp otp) => dbContext.Otps.Remove(otp);

        public void Update(Otp otp) => dbContext.Otps.Update(otp);
        public async Task RemoveOldOtpsAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
        {
            var otps = await dbContext.Otps
                .Where(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose &&
                    !x.IsUsed)
                .ToListAsync(cancellationToken);

            dbContext.Otps.RemoveRange(otps);
        }
    }
}
