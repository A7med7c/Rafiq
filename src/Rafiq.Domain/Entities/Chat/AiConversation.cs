using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Entities.Chat;

public class AiConversation : BaseEntity
{
    protected AiConversation() { } // Required for EF Core

    public AiConversation(Guid userId, Guid userHealthProfileId, string title)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (userHealthProfileId == Guid.Empty)
            throw new ArgumentException("Health Profile ID cannot be empty.", nameof(userHealthProfileId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        UserId = userId;
        UserHealthProfileId = userHealthProfileId;
        Title = title;
    }

    public Guid UserId { get; private set; }
    
    public Guid UserHealthProfileId { get; private set; }
    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public DateTime? LastMessageAt { get; private set; }

    public ICollection<AiMessage> Messages { get; private set; } = new List<AiMessage>();

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty.", nameof(newTitle));

        Title = newTitle;
        MarkUpdated();
    }

    public void MarkMessageActivity(DateTime timestamp)
    {
        LastMessageAt = timestamp;
        MarkUpdated();
    }
}
