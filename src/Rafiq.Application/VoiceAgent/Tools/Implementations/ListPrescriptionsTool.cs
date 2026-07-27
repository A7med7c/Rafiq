using MediatR;
using Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListPrescriptionsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_prescriptions";
    public string Description => "Returns all prescriptions for this profile including medicines, doctor, and date.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["list_medications", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their prescriptions, doctor prescriptions, " +
        "what the doctor prescribed, or 'latest prescription'. Also works for family members.\n" +
        "Arabic triggers: الروشتة، الروشتات، روشتتي، الوصفة الطبية، ايه اللي الدكتور كتبهولي\n" +
        "Arabic family triggers: روشتات ابني، آخر روشتة لأمي، الوصفات الطبية بتاعة أبي\n\n" +
        "Results are ordered by date descending (newest first).\n" +
        "For 'latest prescription' → return only the first item.\n" +
        "Each prescription includes: id, doctorName, hospitalName, date, medicines list.\n\n" +
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

        var result = await mediator.Send(new GetMyPrescriptionsQuery(profileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve prescriptions.");

        var prescriptions = result.Data ?? [];
        return new ToolResult(true,
            prescriptions.Count == 0 ? "No prescriptions found." : $"Found {prescriptions.Count} prescription(s).",
            new { prescriptions });
    }
}
