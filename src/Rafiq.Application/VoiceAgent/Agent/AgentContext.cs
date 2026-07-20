namespace Rafiq.Application.VoiceAgent.Agent;

public sealed record AgentContext(
    Guid UserId,
    Guid ProfileId,
    Guid SessionId,
    string Language = "en",
    /// <summary>Current UTC date — included in system prompt so the AI can resolve
    /// relative dates ("tomorrow", "next Monday") and validate future-date constraints.</summary>
    DateOnly Today = default,
    /// <summary>Current UTC datetime for time-of-day validation.</summary>
    DateTime NowUtc = default);
