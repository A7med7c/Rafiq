using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class AppReview : BaseEntity
{
    protected AppReview() { }

    public AppReview(Guid userId, string displayName, int stars, string? comment,
        string? deviceInfo = null, string? appVersion = null, string? appLanguage = null)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentOutOfRangeException(nameof(stars), "Stars must be between 1 and 5.");

        UserId = userId;
        DisplayName = (displayName?.Trim() is { Length: > 0 } name) ? name : "Anonymous";
        Stars = stars;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        DeviceInfo = deviceInfo;
        AppVersion = appVersion;
        AppLanguage = appLanguage;
    }

    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public int Stars { get; private set; }
    public string? Comment { get; private set; }
    public bool IsVisible { get; private set; } = true;

    public ReviewStatus Status { get; private set; } = ReviewStatus.Pending;
    public ReviewCategory Category { get; private set; } = ReviewCategory.General;

    public string? AdminNotes { get; private set; }
    public string? AdminReply { get; private set; }
    public DateTime? RepliedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public string? DeviceInfo { get; private set; }
    public string? AppVersion { get; private set; }
    public string? AppLanguage { get; private set; }

    public void SetVisible(bool visible) => IsVisible = visible;

    public void UpdateStatus(ReviewStatus status)
    {
        Status = status;
        if (status != ReviewStatus.Pending && ReviewedAt is null)
            ReviewedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void UpdateCategory(ReviewCategory category)
    {
        Category = category;
        MarkUpdated();
    }

    public void UpdateAdminNotes(string? notes)
    {
        AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkUpdated();
    }

    public void SetReply(string? reply)
    {
        AdminReply = string.IsNullOrWhiteSpace(reply) ? null : reply.Trim();
        RepliedAt = AdminReply is null ? null : DateTime.UtcNow;
        MarkUpdated();
    }
}
