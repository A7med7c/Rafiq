using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public class UserNotificationRepository : IUserNotificationRepository
{
    private readonly RafiqDbContext _db;

    public UserNotificationRepository(RafiqDbContext db) => _db = db;

    public async Task AddAsync(UserNotification notification, CancellationToken ct = default)
        => await _db.UserNotifications.AddAsync(notification, ct);

    public async Task<IReadOnlyList<UserNotification>> GetForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await _db.UserNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow),
            ct);
}
