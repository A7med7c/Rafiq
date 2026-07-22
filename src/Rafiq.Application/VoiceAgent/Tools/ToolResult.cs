using Rafiq.Application.VoiceAgent.Domain;

namespace Rafiq.Application.VoiceAgent.Tools;

public sealed record ToolResult(
    bool Success,
    string Message,
    object? Data = null,
    string? ErrorCode = null,

    /// <summary>
    /// The domain entity type this result represents (e.g. "Medication", "Appointment").
    /// When set and the entity registry has a matching IDomainEntityAnalyzer,
    /// the ToolRegistry will analyze the entity data and attach CompletenessGaps.
    /// </summary>
    string? EntityType = null,

    /// <summary>
    /// Populated by the ToolRegistry after running the entity analyzer.
    /// The AI reads these gaps and proactively reasons about whether to suggest follow-up actions.
    /// Not set by tool implementations directly.
    /// </summary>
    IReadOnlyList<CompletenessGap>? CompletenessGaps = null);
