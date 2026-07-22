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

        var summary = result.Data;
        if (summary is null || !summary.HasData)
            return new ToolResult(true, "No health data found for this profile.", new { hasData = false, summary = (string?)null });

        return new ToolResult(true, "Health summary retrieved.", new { hasData = true, summary = summary.Summary });
    }
}
