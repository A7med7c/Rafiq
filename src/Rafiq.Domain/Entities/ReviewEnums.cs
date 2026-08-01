namespace Rafiq.Domain.Entities;

public enum ReviewStatus
{
    Pending = 0,
    Reviewed = 1,
    Resolved = 2,
    Archived = 3
}

public enum ReviewCategory
{
    General = 0,
    BugReport = 1,
    FeatureRequest = 2,
    Performance = 3,
    UiUx = 4,
    ContentQuality = 5
}
