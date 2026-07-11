namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// The fixed allowlist of operations the assistant may request against a category.
/// </summary>
public enum HealthQueryOperation
{
    List,
    Count,
    Exists,
    GetLatest,
    GetOldest,
    GetNext,
    GetPrevious,
    Summary,

    /// <summary>The question spans multiple categories with no single shared operation; each category renders its own default view.</summary>
    Multiple
}
