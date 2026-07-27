using MediatR;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListAllergiesTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_allergies";
    public string Description => "Returns all allergies for this profile including name and severity.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["get_health_summary", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their allergies, drug allergies, food allergies, " +
        "or wants to know what they are allergic to. Also works for family members.\n" +
        "Arabic triggers: حساسية، حساسيات، حساسيتي، ايه الحاجات اللي عندي حساسية منها\n" +
        "Arabic family triggers: حساسيات ابني، الحساسية بتوع ابنتي، ايه الحاجات اللي أبي عنده حساسية منها\n\n" +
        "Result fields per allergy: id, name, severity.\n" +
        "Severity values: Mild, Moderate, Severe.\n\n" +
        "If the user wants to ADD an allergy, tell them to use the health profile section in the app.\n\n" +
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

        var allergies = result.Data?.Allergies ?? [];
        return new ToolResult(true,
            allergies.Count == 0 ? "No allergies recorded." : $"Found {allergies.Count} allergy(ies).",
            new { allergies });
    }
}
