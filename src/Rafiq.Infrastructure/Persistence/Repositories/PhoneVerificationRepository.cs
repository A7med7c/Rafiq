using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

internal sealed class PhoneVerificationRepository(RafiqDbContext _context)
        : IPhoneVerificationRepository
{

    public async Task AddAsync(
        PhoneVerification verification,
        CancellationToken cancellationToken)
    {
        await _context.PhoneVerifications.AddAsync(
            verification,
            cancellationToken);
    }

    public void Update(PhoneVerification verification)
    {
        _context.PhoneVerifications.Update(verification);
    }

    public async Task<PhoneVerification?> GetLatestAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.PhoneVerifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken)
    {
        var verification = await _context.PhoneVerifications
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(
                x => x.UserId == userId && !x.IsUsed,
                cancellationToken);

        if (verification is null)
            return;

        _context.PhoneVerifications.Remove(verification);
    }
    public async Task<bool> ExistsActiveCodeAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.PhoneVerifications.AnyAsync(
            x => x.UserId == userId &&
                 !x.IsUsed &&
                 x.ExpiresAt > DateTime.UtcNow,
            cancellationToken);
    }
}