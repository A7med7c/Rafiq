using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Chat;

public class AiMessage : BaseEntity
{
    protected AiMessage() { } // Required for EF Core

    public AiMessage(Guid aiConversationId, AiMessageRole role, string content, int sequenceNumber)
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
    }

    public Guid AiConversationId { get; private set; }
    public AiConversation AiConversation { get; private set; } = null!;

    public AiMessageRole Role { get; private set; }

    public string Content { get; private set; } = null!;

    public int SequenceNumber { get; private set; }
}
