using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class UserNotification : BaseEntity
{
    protected UserNotification() { }

    public UserNotification(Guid userId, string title, string body, string type = "system")
    {
        UserId = userId;
        Title = title.Trim();
        Body = body.Trim();
        Type = type;
    }

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string Type { get; private set; } = "system";
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
