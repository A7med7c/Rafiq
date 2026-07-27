using MediatR;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class GetProfileInfoTool(ISender mediator) : IVoiceTool
{
    public string Name => "get_profile_info";
    public string Description => "Returns basic profile details: name, date of birth, gender, blood type, height, and weight.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["get_health_summary", "list_allergies", "list_chronic_diseases", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their personal health info like height, weight, " +
        "blood type, date of birth, age, or gender. Also when asking about a family member's profile info.\n" +
        "Arabic triggers: طولي، وزني، فصيلة دمي، سني، تاريخ ميلادي، نوع الدم\n" +
        "Arabic family triggers: طول ابني، وزن ابنتي، فصيلة دم أبي\n\n" +
        "Result fields: firstName, lastName, dateOfBirth, gender, bloodType, height (cm), weight (kg).\n\n" +
        "CRITICAL — NULL FIELDS ARE NOT RECORDED:\n" +
        "  If a field is null in the result, it means the user has NOT entered that value.\n" +
        "  • NEVER invent, estimate, or guess a value for a null field.\n" +
        "  • NEVER use typical values based on age, gender, or any assumption.\n" +
        "  • When null → tell the user: '[field] is not recorded for [name].'\n" +
        "    and offer to help them add it via the health profile section.\n" +
        "  ✓ RIGHT: 'Ahmed's height is not recorded. Would you like to add it?'\n" +
        "  ✗ WRONG: 'Ahmed's height is 175 cm.' (when height is null)\n\n" +
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

        var profile = result.Data;
        if (profile is null)
            return new ToolResult(false, "Profile not found.");

        return new ToolResult(true, "Profile info retrieved.", new
        {
            profile.FirstName,
            profile.LastName,
            profile.DateOfBirth,
            profile.Gender,
            profile.BloodType,
            profile.Height,
            profile.Weight
        });
    }
}
