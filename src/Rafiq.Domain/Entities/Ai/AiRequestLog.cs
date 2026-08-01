using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Ai;

/// <summary>
/// Append-only telemetry record for every AI/Bedrock call.
/// Does NOT extend BaseEntity — no soft-delete, no UpdatedAt.
/// </summary>
public sealed class AiRequestLog
{
    private AiRequestLog() { }

    public static AiRequestLog Create(
        AiFeature feature,
        string modelName,
        bool success,
        int durationMs,
        Guid? userId = null,
        string? errorType = null,
        Guid? conversationId = null)
    {
        return new AiRequestLog
        {
            Id             = Guid.NewGuid(),
            Feature        = feature,
            ModelName      = modelName,
            Success        = success,
            DurationMs     = durationMs,
            UserId         = userId,
            ErrorType      = errorType,
            ConversationId = conversationId,
            CreatedAt      = DateTime.UtcNow
        };
    }

    public Guid       Id             { get; private set; }
    public Guid?      UserId         { get; private set; }
    public AiFeature  Feature        { get; private set; }
    public string     ModelName      { get; private set; } = null!;
    public bool       Success        { get; private set; }
    public string?    ErrorType      { get; private set; }
    public int        DurationMs     { get; private set; }
    public Guid?      ConversationId { get; private set; }
    public DateTime   CreatedAt      { get; private set; }
}
