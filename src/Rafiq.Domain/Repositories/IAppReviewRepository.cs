using Rafiq.Domain.Entities;

namespace Rafiq.Domain.Repositories;

public interface IAppReviewRepository
{
    Task AddAsync(AppReview review, CancellationToken cancellationToken = default);
    Task<bool> HasReviewedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppReview>> GetVisibleAsync(int limit = 20, CancellationToken cancellationToken = default);
}
