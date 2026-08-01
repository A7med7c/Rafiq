using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rafiq.Domain.Enums;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Recurring job that finds documents stuck in "Processing" for more than 10 minutes
/// and moves them to "Failed" so the user can retry.
/// </summary>
public sealed class DocumentRecoveryJob(
    RafiqDbContext db,
    ILogger<DocumentRecoveryJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync()
    {
        var stuckBefore = DateTime.UtcNow.AddMinutes(-10);

        var updated = await db.GeneralDocuments
            .Where(d => d.AnalysisStatus == GeneralDocumentStatus.Processing
                        && d.UpdatedAt < stuckBefore)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.AnalysisStatus, GeneralDocumentStatus.Failed)
                .SetProperty(d => d.FailureReason, "Processing timed out. Please retry.")
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));

        if (updated > 0)
            logger.LogWarning("DocumentRecoveryJob reset {Count} stuck document(s) to Failed.", updated);
    }
}
