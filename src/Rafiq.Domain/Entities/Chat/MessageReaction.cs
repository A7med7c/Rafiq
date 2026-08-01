using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Chat;

public sealed class MessageReaction
{
    private MessageReaction() { }

    public MessageReaction(Guid aiMessageId, Guid userId, ReactionType reactionType, string? feedback = null)
    {
        if (aiMessageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(aiMessageId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        Id             = Guid.NewGuid();
        AiMessageId    = aiMessageId;
        UserId         = userId;
        ReactionType   = reactionType;
        Feedback       = feedback;
        CreatedAt      = DateTime.UtcNow;
        TriageStatus   = FeedbackStatus.New;
    }

    public Guid           Id             { get; private set; }
    public Guid           AiMessageId    { get; private set; }
    public AiMessage      AiMessage      { get; private set; } = null!;
    public Guid           UserId         { get; private set; }
    public ReactionType   ReactionType   { get; private set; }
    public string?        Feedback       { get; private set; }
    public DateTime       CreatedAt      { get; private set; }

    // Admin triage fields
    public FeedbackStatus TriageStatus   { get; private set; } = FeedbackStatus.New;
    public string?        Category       { get; private set; }
    public string?        AdminNotes     { get; private set; }

    public void UpdateFeedback(string? feedback) => Feedback = feedback;

    public void Triage(FeedbackStatus status, string? category, string? adminNotes)
    {
        TriageStatus = status;
        Category     = category;
        AdminNotes   = adminNotes;
    }
}
