namespace Rafiq.Domain.Enums;

public enum FeedbackStatus
{
    New           = 1,
    Investigating = 2,  // was UnderReview — DB value unchanged, no migration needed
    Resolved      = 3,
    Ignored       = 4
}
