namespace Rafiq.Application.AI.Resolution;

public abstract record ProfileResolution
{
    private ProfileResolution() { }

    /// <summary>The user is asking about their own health data.</summary>
    public sealed record Self(Guid ProfileId) : ProfileResolution;

    /// <summary>The user is asking about an accessible family member's data.</summary>
    public sealed record FamilyMember(Guid ProfileId, string DisplayName) : ProfileResolution;

    /// <summary>
    /// A specific person was referenced but no accessible profile matches.
    /// Health data must NOT be loaded; the AI should inform the user.
    /// </summary>
    public sealed record NotFound(string HintUsed) : ProfileResolution;

    /// <summary>The resolution pipeline did not find a confident target; caller should treat as Self.</summary>
    public sealed record Unresolved() : ProfileResolution;
}
