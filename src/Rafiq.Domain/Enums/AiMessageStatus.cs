namespace Rafiq.Domain.Enums;

public enum AiMessageStatus
{
    /// <summary>Message has been persisted but the AI loop has not started yet.</summary>
    Pending = 0,

    /// <summary>The background job is actively running the AI loop.</summary>
    Processing = 1,

    /// <summary>The AI loop completed and Content contains the final answer.</summary>
    Completed = 2,

    /// <summary>The AI loop failed; ErrorMessage contains the reason.</summary>
    Failed = 3,
}
