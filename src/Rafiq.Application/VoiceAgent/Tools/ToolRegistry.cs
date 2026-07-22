using System.Text.Json;
using System.Text.Json.Serialization;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Domain;

namespace Rafiq.Application.VoiceAgent.Tools;

/// <summary>
/// Discovers all registered IVoiceTool implementations via DI and dispatches tool calls.
/// After each successful write operation, runs the appropriate IDomainEntityAnalyzer
/// to attach CompletenessGaps to the result — enabling the AI to reason proactively
/// about what's still missing without any hardcoded conversation scripts.
/// </summary>
public sealed class ToolRegistry(
    IEnumerable<IVoiceTool> tools,
    IEnumerable<IDomainEntityAnalyzer> analyzers)
{
    // Camel-case matches the property names used in entity analyzers ("id", "notes", etc.)
    private static readonly JsonSerializerOptions _analyzeOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IReadOnlyList<IVoiceTool> _tools = tools.ToList();

    private readonly IReadOnlyDictionary<string, IDomainEntityAnalyzer> _analyzers =
        analyzers.ToDictionary(a => a.EntityType, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IVoiceTool> GetAll() => _tools;

    public IVoiceTool? Get(string name) =>
        _tools.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Executes a tool and, if the result declares an EntityType, runs the registered
    /// entity analyzer to produce CompletenessGaps. The agent loop serializes these
    /// gaps into the conversation so the AI can reason about them.
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(
        IVoiceTool tool, ToolCallRequest request, AgentContext context, CancellationToken ct)
    {
        var result = await tool.ExecuteAsync(request, context, ct);

        if (!result.Success || result.EntityType is null || result.Data is null)
            return result;

        if (!_analyzers.TryGetValue(result.EntityType, out var analyzer))
            return result;

        try
        {
            var json = JsonSerializer.Serialize(result.Data, _analyzeOpts);
            using var doc = JsonDocument.Parse(json);
            var gaps = analyzer.Analyze(doc.RootElement);
            return gaps.Count > 0 ? result with { CompletenessGaps = gaps } : result;
        }
        catch
        {
            // Analysis failure must never break the main operation
            return result;
        }
    }
}
