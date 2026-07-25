using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class AppReview : BaseEntity
{
    protected AppReview() { }

    public AppReview(Guid userId, string displayName, int stars, string? comment)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentOutOfRangeException(nameof(stars), "Stars must be between 1 and 5.");

        UserId = userId;
        DisplayName = (displayName?.Trim() is { Length: > 0 } name) ? name : "Anonymous";
        Stars = stars;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public int Stars { get; private set; }
    public string? Comment { get; private set; }
    public bool IsVisible { get; private set; } = true;

    public void SetVisible(bool visible) => IsVisible = visible;
}
