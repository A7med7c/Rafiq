using MediatR;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListChronicDiseasesTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_chronic_diseases";
    public string Description => "Returns all chronic diseases for this profile including name, status, and diagnosis date.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["get_health_summary", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their chronic diseases, chronic conditions, " +
        "or long-term illnesses (diabetes, hypertension, asthma, etc.). Also works for family members.\n" +
        "Arabic triggers: أمراض مزمنة، الأمراض المزمنة، عندي ايه مزمن، السكر، الضغط\n" +
        "Arabic family triggers: أمراض ابني المزمنة، الأمراض المزمنة بتاعة أمي، عند أبي ايه مزمن\n\n" +
        "Result fields per disease: id, name, status, diagnosedAt.\n" +
        "Status values: Active, Controlled, InRemission.\n\n" +
        "If the user wants to ADD a chronic disease, tell them to use the health profile section in the app.\n\n" +
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

        var result = await mediator.Send(new GetPatientProfileByIdQuery(profileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve profile.");

        var diseases = result.Data?.ChronicDiseases ?? [];
        return new ToolResult(true,
            diseases.Count == 0 ? "No chronic diseases recorded." : $"Found {diseases.Count} chronic disease(s).",
            new { chronicDiseases = diseases });
    }
}
