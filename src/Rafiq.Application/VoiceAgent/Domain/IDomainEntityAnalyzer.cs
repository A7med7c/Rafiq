using System.Text.Json;

namespace Rafiq.Application.VoiceAgent.Domain;

/// <summary>
/// Examines the JSON state of a domain entity after a write operation and identifies
/// completeness gaps — aspects that are missing or incomplete and that the AI should
/// reason about proactively.
///
/// To expose a new entity's state to the AI, create a class implementing this interface
/// and register it in VoiceAgentServiceExtensions. No other changes are needed.
/// </summary>
public interface IDomainEntityAnalyzer
{
    /// <summary>
    /// The entity type name this analyzer handles (e.g. "Medication", "Appointment").
    /// Must match the EntityType string returned in ToolResult.
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Inspects the entity's JSON data and returns a list of completeness gaps.
    /// Return an empty list if the entity is fully configured and no follow-up is needed.
    /// </summary>
    IReadOnlyList<CompletenessGap> Analyze(JsonElement entityData);
}
