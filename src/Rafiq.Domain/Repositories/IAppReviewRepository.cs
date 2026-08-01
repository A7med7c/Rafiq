using Rafiq.Domain.Entities;

namespace Rafiq.Domain.Repositories;

public interface IAppReviewRepository
{
    Task AddAsync(AppReview review, CancellationToken cancellationToken = default);
    Task<bool> HasReviewedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppReview>> GetVisibleAsync(int limit = 20, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AppReview> Items, int Total)> GetFilteredAsync(
        int page, int pageSize,
        ReviewStatus? status = null,
        ReviewCategory? category = null,
        int? minStars = null,
        int? maxStars = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default);

    Task<AppReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReviewOverview> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewTrendPoint>> GetTrendsAsync(int months = 6, CancellationToken cancellationToken = default);
    Task<ReviewStats> GetStatsAsync(CancellationToken cancellationToken = default);
    void Remove(AppReview review);
}

public sealed record ReviewOverview(
    int Total,
    int Pending,
    int ThisWeek,
    double AverageStars,
    double HealthScore,
    double PositiveRate,
    int WithReply,
    int FiveStars,
    int FourStars,
    int ThreeStars,
    int TwoStars,
    int OneStar,
    int Visible,
    int Hidden,
    IReadOnlyDictionary<ReviewCategory, int> ByCategory,
    IReadOnlyDictionary<ReviewStatus, int> ByStatus);

public sealed record ReviewTrendPoint(string Month, double AverageStars, int Count);

// Keep old ReviewStats record for backward compat with existing dashboard query
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
