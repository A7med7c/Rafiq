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
    DateTime NowUtc = default,
    /// <summary>
    /// Client's UTC offset in minutes (e.g. +180 for Egypt UTC+3, -300 for US EST UTC-5).
    /// Used to compute the user's local time so the AI can convert times the user expresses
    /// in local terms ("5 PM") into UTC before calling any tool.
    /// </summary>
    int UtcOffsetMinutes = 0);
