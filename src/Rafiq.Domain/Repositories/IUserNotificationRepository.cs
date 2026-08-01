using Rafiq.Domain.Entities;

namespace Rafiq.Domain.Repositories;

public interface IUserNotificationRepository
{
    Task AddAsync(UserNotification notification, CancellationToken ct = default);
    Task<IReadOnlyList<UserNotification>> GetForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<UserNotification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
