using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Repositories;

public interface IHealthSummaryCacheRepository
{
    /// <summary>Returns the cached entry, or null if none exists yet.</summary>
    Task<HealthSummaryCache?> GetAsync(Guid profileId, string language, CancellationToken cancellationToken);

    /// <summary>
    /// Persists (insert or update) the given entry and commits immediately.
    /// Called from the generator handler after a fresh AI response.
    /// </summary>
    Task SaveAsync(HealthSummaryCache entry, CancellationToken cancellationToken);

    /// <summary>
    /// Sets NeedsRefresh = true for every language variant of a profile.
    /// Uses a direct SQL UPDATE so it does NOT require a UnitOfWork.SaveChanges call.
    /// Safe to call after the main UoW has already committed.
    /// </summary>
    Task MarkNeedsRefreshAsync(Guid profileId, CancellationToken cancellationToken);
}
