using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Ai;
using Rafiq.Domain.Enums;
using Rafiq.Infrastructure.Persistence;

namespace Rafiq.Infrastructure.Services.Ai;

/// <summary>
/// Singleton writer that creates its own DI scope for each log entry.
/// This isolates the write from the caller's scope (which may be cancelled
/// or mid-transaction) and ensures failure logs are never lost.
/// </summary>
public sealed class AiRequestLogWriter(
    IServiceScopeFactory scopeFactory,
    ILogger<AiRequestLogWriter> logger) : IAiRequestLogWriter
{
    public async Task WriteAsync(
        AiFeature feature,
        string    modelName,
        bool      success,
        int       durationMs,
        Guid?     userId         = null,
        string?   errorType      = null,
        Guid?     conversationId = null)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RafiqDbContext>();

            var log = AiRequestLog.Create(
                feature, modelName, success, durationMs,
                userId, errorType, conversationId);

            db.AiRequestLogs.Add(log);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Telemetry must never crash the caller — swallow and log.
            logger.LogWarning(ex, "Failed to persist AiRequestLog for feature {Feature}", feature);
        }
    }
}
