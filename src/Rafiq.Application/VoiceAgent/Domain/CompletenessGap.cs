namespace Rafiq.Application.VoiceAgent.Domain;

/// <summary>
/// Describes an aspect of an entity that is absent or incomplete after an operation.
/// The AI reads this data and uses its own reasoning to formulate natural suggestions —
/// it does NOT follow a script derived from this record.
/// </summary>
public sealed record CompletenessGap(
    /// <summary>What is missing (e.g. "No reminders configured").</summary>
    string Description,

    /// <summary>
    /// Why it matters to the user, in plain language
    /// (e.g. "You won't be notified when it's time to take this medication").
    /// The AI uses this to decide whether the gap is worth surfacing.
    /// </summary>
    string Impact,

    /// <summary>Name of the tool that can fill this gap.</summary>
    string SuggestedTool,

    /// <summary>
    /// Parameters the AI already knows and can pre-fill when calling SuggestedTool.
    /// Saves the user from re-answering questions the system already knows the answer to.
    /// </summary>
    IReadOnlyDictionary<string, object>? KnownParameters = null);
