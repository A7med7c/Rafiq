using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class HealthSummaryCacheRepository(RafiqDbContext db) : IHealthSummaryCacheRepository
{
    public Task<HealthSummaryCache?> GetAsync(
        Guid profileId, string language, CancellationToken cancellationToken)
        => db.HealthSummaryCaches
            .FirstOrDefaultAsync(
                x => x.UserHealthProfileId == profileId && x.Language == language.ToLowerInvariant(),
                cancellationToken);

    public async Task SaveAsync(HealthSummaryCache entry, CancellationToken cancellationToken)
    {
        var existing = await db.HealthSummaryCaches
            .FirstOrDefaultAsync(
                x => x.UserHealthProfileId == entry.UserHealthProfileId && x.Language == entry.Language,
                cancellationToken);

        if (existing is null)
            db.HealthSummaryCaches.Add(entry);
        else
            existing.Refresh(entry.SummaryJson);

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task MarkNeedsRefreshAsync(Guid profileId, CancellationToken cancellationToken)
        => db.HealthSummaryCaches
            .Where(x => x.UserHealthProfileId == profileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.NeedsRefresh, true),
                cancellationToken);
}
