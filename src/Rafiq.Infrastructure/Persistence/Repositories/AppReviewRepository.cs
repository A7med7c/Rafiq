using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class AppReviewRepository : IAppReviewRepository
{
    private readonly RafiqDbContext _context;

    public AppReviewRepository(RafiqDbContext context) => _context = context;

    public Task AddAsync(AppReview review, CancellationToken cancellationToken = default)
        => _context.AppReviews.AddAsync(review, cancellationToken).AsTask();

    public Task<bool> HasReviewedAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.AppReviews.AnyAsync(r => r.UserId == userId && !r.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<AppReview>> GetVisibleAsync(int limit = 20, CancellationToken cancellationToken = default)
        => await _context.AppReviews
            .Where(r => r.IsVisible && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<AppReview> Items, int Total)> GetFilteredAsync(
        int page, int pageSize,
        ReviewStatus? status = null,
        ReviewCategory? category = null,
        int? minStars = null,
        int? maxStars = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var q = _context.AppReviews.Where(r => !r.IsDeleted);

        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);

        if (category.HasValue)
            q = q.Where(r => r.Category == category.Value);

        if (minStars.HasValue)
            q = q.Where(r => r.Stars >= minStars.Value);

        if (maxStars.HasValue)
            q = q.Where(r => r.Stars <= maxStars.Value);

        q = sortBy switch
        {
            "stars_asc"  => q.OrderBy(r => r.Stars).ThenByDescending(r => r.CreatedAt),
            "stars_desc" => q.OrderByDescending(r => r.Stars).ThenByDescending(r => r.CreatedAt),
            "oldest"     => q.OrderBy(r => r.CreatedAt),
            _            => q.OrderByDescending(r => r.CreatedAt)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<AppReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.AppReviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<ReviewOverview> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var all = await _context.AppReviews
            .Where(r => !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (all.Count == 0)
            return new ReviewOverview(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<ReviewCategory, int>(), new Dictionary<ReviewStatus, int>());

        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        // Recency-weighted health score (1.0 = last 30d, 0.5 = 30-90d, 0.25 = older)
        var (weightedSum, weightTotal) = all.Aggregate((0.0, 0.0), (acc, r) =>
        {
            var age = (now - r.CreatedAt).TotalDays;
            var w = age <= 30 ? 1.0 : age <= 90 ? 0.5 : 0.25;
            return (acc.Item1 + r.Stars * w, acc.Item2 + 5 * w);
        });
        var healthScore = weightTotal > 0 ? Math.Round(weightedSum / weightTotal * 100, 1) : 0;

        var positive = all.Count(r => r.Stars >= 4);
        var positiveRate = Math.Round((double)positive / all.Count * 100, 1);

        var byCategory = all
            .GroupBy(r => r.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var byStatus = all
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ReviewOverview(
            Total:       all.Count,
            Pending:     all.Count(r => r.Status == ReviewStatus.Pending),
            ThisWeek:    all.Count(r => r.CreatedAt >= weekAgo),
            AverageStars: Math.Round(all.Average(r => r.Stars), 1),
            HealthScore: healthScore,
            PositiveRate: positiveRate,
            WithReply:   all.Count(r => r.AdminReply != null),
            FiveStars:   all.Count(r => r.Stars == 5),
            FourStars:   all.Count(r => r.Stars == 4),
            ThreeStars:  all.Count(r => r.Stars == 3),
            TwoStars:    all.Count(r => r.Stars == 2),
            OneStar:     all.Count(r => r.Stars == 1),
            Visible:     all.Count(r => r.IsVisible),
            Hidden:      all.Count(r => !r.IsVisible),
            ByCategory:  byCategory,
            ByStatus:    byStatus);
    }

    public async Task<IReadOnlyList<ReviewTrendPoint>> GetTrendsAsync(
        int months = 6, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months).Date;

        var raw = await _context.AppReviews
            .Where(r => !r.IsDeleted && r.CreatedAt >= cutoff)
            .Select(r => new { r.Stars, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var grouped = raw
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ReviewTrendPoint(
                Month: new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                AverageStars: Math.Round(g.Average(r => r.Stars), 1),
                Count: g.Count()))
            .ToList();

        return grouped;
    }

    public async Task<ReviewStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _context.AppReviews
            .Where(r => !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (all.Count == 0)
            return new ReviewStats(0, 0, 0, 0, 0, 0, 0, 0, 0);

        return new ReviewStats(
            Total:        all.Count,
            Visible:      all.Count(r => r.IsVisible),
            Hidden:       all.Count(r => !r.IsVisible),
            AverageStars: Math.Round(all.Average(r => r.Stars), 1),
            FiveStars:    all.Count(r => r.Stars == 5),
            FourStars:    all.Count(r => r.Stars == 4),
            ThreeStars:   all.Count(r => r.Stars == 3),
            TwoStars:     all.Count(r => r.Stars == 2),
            OneStar:      all.Count(r => r.Stars == 1));
    }

    public void Remove(AppReview review)
        => _context.AppReviews.Remove(review);
}
