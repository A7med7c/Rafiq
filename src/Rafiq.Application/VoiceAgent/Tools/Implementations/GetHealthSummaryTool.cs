using MediatR;
using Rafiq.Application.Features.AiChat.Queries.GenerateHealthSummary;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class GetHealthSummaryTool(ISender mediator) : IVoiceTool
{
    public string Name => "get_health_summary";
    public string Description => "Returns a comprehensive health summary: profile info, allergies, chronic diseases, and recent health activity.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";
    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when the user asks: 'what do you know about me', 'my health summary', " +
        "'my profile', or any general health overview question.\n" +
        "The summary is pre-formatted text — read it back naturally to the user.\n" +
        "If hasData is false, the profile has no health data yet and you should suggest " +
        "they complete their profile first.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GenerateHealthSummaryQuery(context.ProfileId, context.Language), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve health summary.");

        var s = result.Data;
        if (s is null || !s.HasData)
            return new ToolResult(true, "No health data found for this profile.", new { hasData = false, summary = (string?)null });

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"Overall status: {s.OverallStatus}{(s.OverallStatusNote is not null ? " — " + s.OverallStatusNote : "")}");

        lines.AppendLine(s.Conditions.Count > 0
            ? $"Conditions: {string.Join(", ", s.Conditions)}"
            : "Conditions: none recorded");

        lines.AppendLine(s.Allergies.Count > 0
            ? $"Allergies: {string.Join(", ", s.Allergies.Select(a => $"{a.Name} ({a.Severity})"))}"
            : "Allergies: none recorded");

        lines.AppendLine($"Medications: {s.Medications.Count} active" +
            (s.Medications.HasIssues && s.Medications.IssueNote is not null ? $" — {s.Medications.IssueNote}" : ""));

        lines.AppendLine($"Lab results: {s.LabResults.Status}" +
            (s.LabResults.AbnormalCount > 0 ? $" ({s.LabResults.AbnormalCount} abnormal)" : ""));

        if (s.Insights.Count > 0)
            lines.AppendLine($"Insights: {string.Join("; ", s.Insights)}");

        if (s.Recommendations.Count > 0)
            lines.AppendLine($"Recommendations: {string.Join("; ", s.Recommendations)}");

        return new ToolResult(true, "Health summary retrieved.", new { hasData = true, summary = lines.ToString().TrimEnd() });
    }
}
