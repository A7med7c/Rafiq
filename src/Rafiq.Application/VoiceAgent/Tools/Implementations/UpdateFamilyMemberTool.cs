using MediatR;
using Rafiq.Application.Features.PatientProfiles.Commands.UpdatePatientProfile;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class UpdateFamilyMemberTool(ISender mediator) : IVoiceTool
{
    public string Name => "update_family_member";
    public string Description => "Updates the health profile of an existing family member. All core fields (firstName, lastName, dateOfBirth, gender) must be submitted even if unchanged.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"profileId\",\"firstName\",\"lastName\",\"dateOfBirth\",\"gender\"]," +
        "\"properties\":{" +
        "\"profileId\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"firstName\":{\"type\":\"string\",\"maxLength\":100}," +
        "\"lastName\":{\"type\":\"string\",\"maxLength\":100}," +
        "\"dateOfBirth\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"gender\":{\"type\":\"string\",\"enum\":[\"Male\",\"Female\"]}," +
        "\"relationship\":{\"type\":\"string\",\"enum\":[\"Son\",\"Daughter\",\"Father\",\"Mother\",\"Husband\",\"Wife\",\"Brother\",\"Sister\",\"Grandfather\",\"Grandmother\",\"Other\"]}," +
        "\"bloodType\":{\"type\":\"string\",\"enum\":[\"APositive\",\"ANegative\",\"BPositive\",\"BNegative\",\"ABPositive\",\"ABNegative\",\"OPositive\",\"ONegative\"]}," +
        "\"height\":{\"type\":\"number\",\"minimum\":30,\"maximum\":300}," +
        "\"weight\":{\"type\":\"number\",\"minimum\":1,\"maximum\":500}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — IDENTIFY the family member.\n" +
        "    Call list_family_profiles to find their userHealthProfileId.\n" +
        "    Apply disambiguation if multiple members match.\n\n" +

        "  STEP 2 — FETCH current values.\n" +
        "    Call get_family_member_details { profileId } to read all current field values.\n" +
        "    This is required because the update command replaces ALL fields — not just changed ones.\n\n" +

        "  STEP 3 — ASK what the user wants to change.\n" +
        "    Only ask about fields the user hasn't mentioned yet.\n" +
        "    For unchanged fields, keep the values from get_family_member_details.\n\n" +

        "  STEP 4 — CONFIRM the changes before updating.\n" +
        "    Show only the fields that differ from the current values:\n" +
        "    EN: 'I'll update Mohamed's profile: weight 32 kg → 35 kg. Confirm?'\n" +
        "    AR: 'سأحدث ملف محمد: الوزن من 32 كجم إلى 35 كجم. هل تؤكد؟'\n\n" +

        "  STEP 5 — CALL update_family_member with ALL required fields.\n" +
        "    Submit unchanged fields with their current values from Step 2.\n\n" +

        "REQUIRED — all five must be submitted even if unchanged:\n" +
        "  profileId     — userHealthProfileId from list_family_profiles\n" +
        "  firstName     — Max 100 characters.\n" +
        "  lastName      — Max 100 characters.\n" +
        "  dateOfBirth   — YYYY-MM-DD, must not be in the future.\n" +
        "  gender        — 'Male' or 'Female'.\n\n" +

        "OPTIONAL — omit to leave unchanged; pass explicitly to update:\n" +
        "  relationship  — Son, Daughter, Father, Mother, Husband, Wife, Brother, Sister,\n" +
        "                  Grandfather, Grandmother, Other.\n" +
        "                  'Self' is never allowed.\n" +
        "  bloodType     — APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative,\n" +
        "                  OPositive, ONegative. Pass null to clear.\n" +
        "  height        — cm (30–300). Pass null to clear.\n" +
        "  weight        — kg (1–500). Pass null to clear.\n\n" +

        "BACKEND ENFORCES (relay any error; never pre-validate):\n" +
        "  - dateOfBirth cannot be in the future.\n" +
        "  - height 30–300 cm, weight 1–500 kg.\n" +
        "  - Only the profile Owner can change the relationship field.\n" +
        "  - Viewer access cannot update any field.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;

        if (!Guid.TryParse(p.GetString("profileId"), out var profileId))
            return new ToolResult(false, "Invalid profileId. Provide a UUID from list_family_profiles.");

        if (!Enum.TryParse<Gender>(p.GetString("gender"), ignoreCase: true, out var gender))
            return new ToolResult(false, "Invalid gender. Use 'Male' or 'Female'.");

        if (!DateOnly.TryParse(p.GetString("dateOfBirth"), out var dateOfBirth))
            return new ToolResult(false, "Invalid dateOfBirth. Use YYYY-MM-DD format.");

        RelationshipType? relationship = null;
        if (p.TryGetProperty("relationship", out var relProp) && relProp.GetString() is { } relStr)
        {
            if (!Enum.TryParse<RelationshipType>(relStr, ignoreCase: true, out var rel)
                || rel == RelationshipType.Self)
                return new ToolResult(false,
                    "Invalid relationship. Valid values: Son, Daughter, Father, Mother, Husband, Wife, Brother, Sister, Grandfather, Grandmother, Other.");
            relationship = rel;
        }

        BloodType? bloodType = null;
        if (p.TryGetProperty("bloodType", out var btProp) && btProp.GetString() is { } btStr)
        {
            if (!Enum.TryParse<BloodType>(btStr, ignoreCase: true, out var bt))
                return new ToolResult(false,
                    "Invalid bloodType. Valid values: APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative, OPositive, ONegative.");
            bloodType = bt;
        }

        decimal? height = p.TryGetProperty("height", out var hProp) ? hProp.GetDecimal() : null;
        decimal? weight = p.TryGetProperty("weight", out var wProp) ? wProp.GetDecimal() : null;

        try
        {
            var command = new UpdatePatientProfileCommand(
                profileId,
                p.GetString("firstName") ?? string.Empty,
                p.GetString("lastName") ?? string.Empty,
                dateOfBirth,
                gender,
                bloodType,
                height,
                weight,
                relationship);

            var result = await mediator.Send(command, cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to update profile.");

            return new ToolResult(true,
                $"Profile for {p.GetString("firstName")} updated successfully.",
                result.Data);
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
    }
}
