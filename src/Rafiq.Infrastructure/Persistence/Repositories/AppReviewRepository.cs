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
}
