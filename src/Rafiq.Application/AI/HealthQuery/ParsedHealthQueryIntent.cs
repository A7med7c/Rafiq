namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// A validated, allowlist-safe health query intent. Every value here has already been
/// checked against the fixed enums - nothing from the raw AI output survives into this
/// type unless it matched a known category/operation/timeframe.
/// </summary>
public sealed record ParsedHealthQueryIntent(
    IReadOnlyList<HealthQueryCategory> Categories,
    HealthQueryOperation Operation,
    string? SearchTerm,
    HealthQueryTimeframe Timeframe)
{
    public static ParsedHealthQueryIntent Empty { get; } = new(
        Array.Empty<HealthQueryCategory>(),
        HealthQueryOperation.List,
        null,
        HealthQueryTimeframe.None);

    public bool HasNoCategories => Categories.Count == 0;
}
