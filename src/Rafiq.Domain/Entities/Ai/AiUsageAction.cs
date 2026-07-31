namespace Rafiq.Domain.Entities.Ai;

/// <summary>
/// Records an admin action taken after reviewing a user's flagged AI usage.
/// Append-only — no soft-delete, no BaseEntity.
/// </summary>
public sealed class AiUsageAction
{
    private AiUsageAction() { }

    public static AiUsageAction Create(
        Guid targetUserId,
        Guid adminId,
        string adminName,
        string actionType,
        string? notes)
    {
        return new AiUsageAction
        {
            Id           = Guid.NewGuid(),
            TargetUserId = targetUserId,
            AdminId      = adminId,
            AdminName    = adminName,
            ActionType   = actionType,
            Notes        = notes,
            CreatedAt    = DateTime.UtcNow
        };
    }

    public Guid     Id           { get; private set; }
    public Guid     TargetUserId { get; private set; }
    public Guid     AdminId      { get; private set; }
    public string   AdminName    { get; private set; } = null!;
    public string   ActionType   { get; private set; } = null!;
    public string?  Notes        { get; private set; }
    public DateTime CreatedAt    { get; private set; }
}
