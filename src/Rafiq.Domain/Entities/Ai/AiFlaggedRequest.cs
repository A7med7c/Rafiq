namespace Rafiq.Domain.Entities.Ai;

/// <summary>
/// Records an AI request that the AI classifier deemed problematic (off-domain, wrong upload, policy violation, etc.).
/// Append-only — no soft-delete, no BaseEntity.
/// </summary>
public sealed class AiFlaggedRequest
{
    private AiFlaggedRequest() { }

    public static AiFlaggedRequest Create(
        Guid userId,
        string requestType,
        string userRequest,
        string aiResponse,
        string classification,
        string reason)
    {
        return new AiFlaggedRequest
        {
            Id             = Guid.NewGuid(),
            UserId         = userId,
            RequestType    = requestType,
            UserRequest    = userRequest,
            AiResponse     = aiResponse,
            Classification = classification,
            Reason         = reason,
            CreatedAt      = DateTime.UtcNow
        };
    }

    public Guid     Id             { get; private set; }
    public Guid     UserId         { get; private set; }
    public string   RequestType    { get; private set; } = null!;
    public string   UserRequest    { get; private set; } = null!;
    public string   AiResponse     { get; private set; } = null!;
    public string   Classification { get; private set; } = null!;
    public string   Reason         { get; private set; } = null!;
    public DateTime CreatedAt      { get; private set; }
}
