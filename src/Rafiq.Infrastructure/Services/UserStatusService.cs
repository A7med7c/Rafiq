using Microsoft.EntityFrameworkCore;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services;

public sealed class UserStatusService(RafiqDbContext db) : IUserStatusService
{
    public async Task<UserStatus?> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserStatus(u.IsAiRestricted, u.IsRestricted, u.IsSuspended))
            .FirstOrDefaultAsync(ct);

        return result;
    }
}
