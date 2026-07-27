using MediatR;
using Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListLabReportsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_lab_reports";
    public string Description => "Returns all lab reports (blood tests, urine tests, etc.) for this profile.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["list_imaging_reports", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their lab reports, blood tests, test results, " +
        "lab work, analysis results, or 'latest lab report'. Also works for family members.\n" +
        "Arabic triggers: تحاليل، تحليل الدم، نتايج التحاليل، آخر تحليل، تحاليلي\n" +
        "Arabic family triggers: تحاليل ابني، نتايج التحاليل بتاعة أمي، آخر تحليل لأبي\n\n" +
        "Results are ordered by date descending (newest first).\n" +
        "For 'latest lab report' → return only the first item.\n" +
        "Each report includes: id, title, labName, date, results (list of test name + value + unit + reference range).\n\n" +
        "FAMILY MEMBER CONTEXT: When asking about a family member:\n" +
        "  1. Call list_family_profiles first to find their userHealthProfileId\n" +
        "  2. Pass their id as targetProfileId in this tool call";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var profileId = context.ProfileId;
        if (request.Parameters.TryGetProperty("targetProfileId", out var tpidProp)
            && Guid.TryParse(tpidProp.GetString(), out var familyProfileId))
        {
            profileId = familyProfileId;
        }

        var result = await mediator.Send(new GetMyLabReportsQuery(profileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve lab reports.");

        var labReports = result.Data ?? [];
        return new ToolResult(true,
            labReports.Count == 0 ? "No lab reports found." : $"Found {labReports.Count} lab report(s).",
            new { labReports });
    }
}
