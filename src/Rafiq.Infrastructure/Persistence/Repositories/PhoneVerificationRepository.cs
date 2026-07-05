using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

internal sealed class OtpVerificationRepository(RafiqDbContext context)
    : IOtpVerificationRepository
{
    public async Task AddAsync(
        OtpVerification verification,
        CancellationToken cancellationToken)
    {
        await context.PhoneVerifications.AddAsync(
            verification,
            cancellationToken);
    }

    public void Update(OtpVerification verification)
    {
        context.PhoneVerifications.Update(verification);
    }

    public async Task<OtpVerification?> GetLatestAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        return await context.PhoneVerifications
            .Where(x =>
                x.UserId == userId &&
                x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteByUserIdAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        var verification = await context.PhoneVerifications
            .Where(x =>
                x.UserId == userId &&
                x.Purpose == purpose &&
                !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (verification is null)
            return;

        context.PhoneVerifications.Remove(verification);
    }

    public async Task<bool> ExistsActiveCodeAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        return await context.PhoneVerifications.AnyAsync(
            x =>
                x.UserId == userId &&
                x.Purpose == purpose &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow,
            cancellationToken);
    }
}