using Rafiq.Domain.Entities;

namespace Rafiq.Domain.Repositories;

public interface IAppReviewRepository
{
    Task AddAsync(AppReview review, CancellationToken cancellationToken = default);
    Task<bool> HasReviewedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppReview>> GetVisibleAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AppReview> Items, int Total)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AppReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReviewStats> GetStatsAsync(CancellationToken cancellationToken = default);
    void Remove(AppReview review);
}

public sealed record ReviewStats(
    int Total,
    int Visible,
    int Hidden,
    double AverageStars,
    int FiveStars,
    int FourStars,
    int ThreeStars,
    int TwoStars,
    int OneStar);
