using MediatR;
using Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListImagingReportsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_imaging_reports";
    public string Description => "Returns all imaging reports (X-rays, MRI, CT scans, ultrasound) for this profile.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["list_lab_reports", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their imaging reports, X-rays, MRI scans, " +
        "CT scans, ultrasound, or radiology results. Also works for family members.\n" +
        "Arabic triggers: أشعة، رنين، أشعة مقطعية، سونار، الأشعة بتاعتي، نتايج الأشعة\n" +
        "Arabic family triggers: أشعات ابني، الرنين بتاع أمي، نتايج الأشعة بتاعة أبي\n\n" +
        "Results are ordered by date descending (newest first).\n" +
        "For 'latest imaging report' → return only the first item.\n" +
        "Each report includes: id, title, imagingType, facilityName, date, findings, impression.\n\n" +
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

        var result = await mediator.Send(new GetMyImagingReportsQuery(profileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve imaging reports.");

        var imagingReports = result.Data ?? [];
        return new ToolResult(true,
            imagingReports.Count == 0 ? "No imaging reports found." : $"Found {imagingReports.Count} imaging report(s).",
            new { imagingReports });
    }
}
