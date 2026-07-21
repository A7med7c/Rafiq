using MediatR;
using Rafiq.Application.Features.PatientProfiles.Queries.GetPatientProfileById;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class GetFamilyMemberDetailsTool(ISender mediator) : IVoiceTool
{
    public string Name => "get_family_member_details";
    public string Description => "Returns the full health profile details (name, date of birth, gender, blood type, height, weight, allergies, chronic diseases) for a specific family member.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"profileId\"]," +
        "\"properties\":{" +
        "\"profileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"userHealthProfileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when you need the full details of a specific family member before\n" +
        "updating them (you must know their current values to submit all fields correctly).\n\n" +

        "REQUIRED:\n" +
        "  profileId — the userHealthProfileId from list_family_profiles\n\n" +

        "RESULT FIELDS:\n" +
        "  id, firstName, lastName, dateOfBirth (YYYY-MM-DD), gender (Male/Female),\n" +
        "  bloodType (APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative,\n" +
        "             OPositive, ONegative — null if unknown),\n" +
        "  height (cm, null if unknown), weight (kg, null if unknown),\n" +
        "  allergies: [{id, name, severity}],\n" +
        "  chronicDiseases: [{id, name, diagnosedAt, status}]\n\n" +

        "TYPICAL USAGE — always call this before update_family_member:\n" +
        "  1. list_family_profiles → get profileId\n" +
        "  2. get_family_member_details { profileId } → get current field values\n" +
        "  3. Ask the user which fields to change\n" +
        "  4. update_family_member with ALL fields (unchanged ones keep current values)";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("profileId"), out var profileId))
            return new ToolResult(false, "Invalid profileId. Provide a UUID from list_family_profiles.");

        try
        {
            var result = await mediator.Send(new GetPatientProfileByIdQuery(profileId), cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to retrieve profile details.");

            return new ToolResult(true, "Profile details retrieved.", result.Data);
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
