using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Chat;

public class AiMessage : BaseEntity
{
    protected AiMessage() { } // Required for EF Core

    /// <summary>
    /// Standard constructor for completed messages (Voice turns, inline completions).
    /// Status defaults to Completed so existing callers need no changes.
    /// </summary>
    public AiMessage(Guid aiConversationId, AiMessageRole role, string content, int sequenceNumber,
        AiMessageStatus status = AiMessageStatus.Completed)
    {
        if (aiConversationId == Guid.Empty)
            throw new ArgumentException("Conversation ID cannot be empty.", nameof(aiConversationId));

        if (!Enum.IsDefined(typeof(AiMessageRole), role))
            throw new ArgumentException("Invalid message role.", nameof(role));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.", nameof(content));

        if (sequenceNumber <= 0)
            throw new ArgumentException("Sequence number must be greater than zero.", nameof(sequenceNumber));

        AiConversationId = aiConversationId;
        Role = role;
        Content = content;
        SequenceNumber = sequenceNumber;
        Status = status;
    }

    public Guid AiConversationId { get; private set; }
    public AiConversation AiConversation { get; private set; } = null!;

    public AiMessageRole Role { get; private set; }

    public string Content { get; private set; } = null!;

    public int SequenceNumber { get; private set; }

    /// <summary>Lifecycle status — only meaningful for Assistant messages created via the Chat 202 path.</summary>
    public AiMessageStatus Status { get; private set; } = AiMessageStatus.Completed;

    /// <summary>Set when Status == Failed; contains the user-facing error text.</summary>
    public string? ErrorMessage { get; private set; }

    // ── Lifecycle transitions (Chat async path only) ──────────────────────────

    public void SetProcessing()
    {
        Status = AiMessageStatus.Processing;
    }

    public void Complete(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Completed content cannot be empty.", nameof(content));
        Content = content;
        Status = AiMessageStatus.Completed;
    }

    public void Fail(string? errorMessage = null)
    {
        ErrorMessage = errorMessage;
        Status = AiMessageStatus.Failed;
    }
}
