using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Persists a single telemetry row for an AI/Bedrock call.
/// Uses its own DbContext scope so the write succeeds even when the
/// caller's scope is cancelled or mid-transaction.
/// </summary>
public interface IAiRequestLogWriter
{
    Task WriteAsync(
        AiFeature feature,
        string    modelName,
        bool      success,
        int       durationMs,
        Guid?     userId         = null,
        string?   errorType      = null,
        Guid?     conversationId = null);
}
