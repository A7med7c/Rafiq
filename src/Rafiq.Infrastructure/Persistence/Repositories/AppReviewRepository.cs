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

    public async Task<(IReadOnlyList<AppReview> Items, int Total)> GetAllAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.AppReviews
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<AppReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.AppReviews.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

    public async Task<ReviewStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var all = await _context.AppReviews
            .Where(r => !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (all.Count == 0)
            return new ReviewStats(0, 0, 0, 0, 0, 0, 0, 0, 0);

        return new ReviewStats(
            Total:      all.Count,
            Visible:    all.Count(r => r.IsVisible),
            Hidden:     all.Count(r => !r.IsVisible),
            AverageStars: Math.Round(all.Average(r => r.Stars), 1),
            FiveStars:  all.Count(r => r.Stars == 5),
            FourStars:  all.Count(r => r.Stars == 4),
            ThreeStars: all.Count(r => r.Stars == 3),
            TwoStars:   all.Count(r => r.Stars == 2),
            OneStar:    all.Count(r => r.Stars == 1));
    }

    public void Remove(AppReview review)
        => _context.AppReviews.Remove(review);
}
