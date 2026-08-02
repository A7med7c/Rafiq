namespace Rafiq.Domain.Entities.User;

public sealed class HealthSummaryCache
{
    private HealthSummaryCache() { }

    public static HealthSummaryCache Create(Guid profileId, string language, string summaryJson)
        => new()
        {
            Id = Guid.NewGuid(),
            UserHealthProfileId = profileId,
            Language = language.ToLowerInvariant(),
            SummaryJson = summaryJson,
            GeneratedAt = DateTime.UtcNow,
            NeedsRefresh = false,
        };

    public Guid Id { get; private set; }

    public Guid UserHealthProfileId { get; private set; }

    /// <summary>Lowercase BCP-47 tag, e.g. "en" or "ar".</summary>
    public string Language { get; private set; } = null!;

    public string SummaryJson { get; private set; } = null!;

    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// True when health data changed since the last generation.
    /// The next dashboard request will regenerate and clear this flag.
    /// </summary>
    public bool NeedsRefresh { get; private set; }

    public void Refresh(string summaryJson)
    {
        SummaryJson = summaryJson;
        GeneratedAt = DateTime.UtcNow;
        NeedsRefresh = false;
    }
}
