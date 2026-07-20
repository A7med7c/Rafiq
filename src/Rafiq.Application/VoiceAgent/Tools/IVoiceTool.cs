using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools;

public interface IVoiceTool
{
    /// <summary>Snake_case tool name the AI uses in JSON output.</summary>
    string Name { get; }

    /// <summary>One-sentence description of what this tool does.</summary>
    string Description { get; }

    /// <summary>
    /// JSON Schema for the parameters object. The AI uses this to produce valid JSON.
    /// Keep it minimal — full business rules belong in DomainContext.
    /// </summary>
    string ParameterSchema { get; }

    /// <summary>
    /// Rich domain context injected verbatim into the system prompt.
    /// Tells the AI: required vs optional fields, valid enum values, validation rules,
    /// business constraints, and conversation guidance (what to ask when).
    /// This is the bridge between the application's domain model and the AI's intelligence.
    /// </summary>
    string DomainContext { get; }

    /// <summary>
    /// Names of tools the AI should proactively offer to run after this tool succeeds.
    /// Example: after add_medication → suggest add_medication_reminder.
    /// </summary>
    string[] RelatedToolNames { get; }

    Task<ToolResult> ExecuteAsync(
        ToolCallRequest request,
        AgentContext context,
        CancellationToken cancellationToken);
}
