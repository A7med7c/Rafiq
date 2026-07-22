using MediatR;
using Rafiq.Application.Features.PatientProfiles.Commands.CreateManagedProfile;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class AddFamilyMemberTool(ISender mediator) : IVoiceTool
{
    public string Name => "add_family_member";
    public string Description => "Creates a new managed health profile for a family member (someone without their own registered account).";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"firstName\",\"lastName\",\"dateOfBirth\",\"gender\",\"relationship\"]," +
        "\"properties\":{" +
        "\"firstName\":{\"type\":\"string\",\"maxLength\":100}," +
        "\"lastName\":{\"type\":\"string\",\"maxLength\":100}," +
        "\"dateOfBirth\":{\"type\":\"string\",\"format\":\"date\",\"description\":\"YYYY-MM-DD, must not be in the future\"}," +
        "\"gender\":{\"type\":\"string\",\"enum\":[\"Male\",\"Female\"]}," +
        "\"relationship\":{\"type\":\"string\",\"enum\":[\"Son\",\"Daughter\",\"Father\",\"Mother\",\"Husband\",\"Wife\",\"Brother\",\"Sister\",\"Grandfather\",\"Grandmother\",\"Other\"]}," +
        "\"bloodType\":{\"type\":\"string\",\"enum\":[\"APositive\",\"ANegative\",\"BPositive\",\"BNegative\",\"ABPositive\",\"ABNegative\",\"OPositive\",\"ONegative\"]}," +
        "\"height\":{\"type\":\"number\",\"minimum\":30,\"maximum\":300,\"description\":\"Height in cm\"}," +
        "\"weight\":{\"type\":\"number\",\"minimum\":1,\"maximum\":500,\"description\":\"Weight in kg\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL to add a family member who does NOT have their own Rafiq account.\n" +
        "For members with an existing account, use the invitation system instead.\n\n" +

        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — COLLECT all five required fields.\n" +
        "    Often the user gives several at once (e.g. 'Add my 10-year-old son Mohamed').\n" +
        "    Extract: firstName='Mohamed', relationship='Son', dateOfBirth can be inferred.\n" +
        "    Ask only for what is still missing. Never ask for a field already provided.\n\n" +

        "  STEP 2 — OFFER optional medical details (ask at most once each):\n" +
        "    'Do you know their blood type?' (skip if user says no/skip/لا/تجاوز)\n" +
        "    'What is their height and weight?' (optional, skip freely)\n" +
        "    Do NOT ask about allergies or chronic diseases — those can be added later.\n\n" +

        "  STEP 3 — CONFIRM before creating:\n" +
        "    Show a summary, then ask for confirmation.\n" +
        "    EN example:\n" +
        "      'I'll add a profile for:\n" +
        "       • Name: Mohamed Ali\n" +
        "       • Relationship: Son\n" +
        "       • Date of birth: 2014-03-15\n" +
        "       • Gender: Male\n" +
        "       Shall I go ahead?'\n" +
        "    AR example:\n" +
        "      'سأضيف ملف صحي لـ:\n" +
        "       • الاسم: محمد علي\n" +
        "       • الصلة: ابن\n" +
        "       • تاريخ الميلاد: 2014-03-15\n" +
        "       • الجنس: ذكر\n" +
        "       هل تريد المتابعة؟'\n\n" +

        "  STEP 4 — CALL add_family_member after explicit confirmation.\n\n" +

        "REQUIRED — must be provided by the user; never invent or guess:\n" +
        "  firstName     — given name. Max 100 characters.\n" +
        "  lastName      — family name. Max 100 characters.\n" +
        "  dateOfBirth   — YYYY-MM-DD. Must be today or in the past.\n" +
        "                  If user says 'he is 10 years old', compute approximately:\n" +
        "                  dateOfBirth ≈ TODAY - 10 years (e.g. 2015-01-01 — ask to confirm).\n" +
        "                  If user gives birth year only ('born in 2012'), use 2012-01-01.\n" +
        "                  Always confirm the computed date before calling the tool.\n" +
        "  gender        — 'Male' or 'Female'. Often implied by relationship (Son→Male, Daughter→Female).\n" +
        "                  Confirm if not explicit.\n" +
        "  relationship  — the user's relationship to this person:\n" +
        "                  Son, Daughter, Father, Mother, Husband, Wife, Brother, Sister,\n" +
        "                  Grandfather, Grandmother, Other.\n" +
        "                  'Self' is NOT allowed for managed profiles.\n\n" +

        "OPTIONAL — ask at most once; skip freely if user declines or ignores:\n" +
        "  bloodType — APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative,\n" +
        "              OPositive, ONegative. Omit if unknown.\n" +
        "  height    — in centimetres (30–300). Omit if not provided.\n" +
        "  weight    — in kilograms (1–500). Omit if not provided.\n\n" +

        "BACKEND ENFORCES (never pre-validate; relay any error):\n" +
        "  - dateOfBirth cannot be in the future.\n" +
        "  - height must be 30–300 cm when provided.\n" +
        "  - weight must be 1–500 kg when provided.\n" +
        "  - relationship cannot be Self.\n\n" +

        "RESULT: The new profile's id is returned. Use it as targetProfileId to immediately\n" +
        "act on this profile (e.g. add their medications or book an appointment).";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;

        if (!Enum.TryParse<Gender>(p.GetString("gender"), ignoreCase: true, out var gender))
            return new ToolResult(false, "Invalid gender. Use 'Male' or 'Female'.");

        if (!Enum.TryParse<RelationshipType>(p.GetString("relationship"), ignoreCase: true, out var relationship)
            || relationship == RelationshipType.Self)
            return new ToolResult(false,
                "Invalid relationship. Valid values: Son, Daughter, Father, Mother, Husband, Wife, Brother, Sister, Grandfather, Grandmother, Other.");

        if (!DateOnly.TryParse(p.GetString("dateOfBirth"), out var dateOfBirth))
            return new ToolResult(false, "Invalid dateOfBirth. Use YYYY-MM-DD format.");

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
            var command = new CreateManagedProfileCommand(
                p.GetString("firstName") ?? string.Empty,
                p.GetString("lastName") ?? string.Empty,
                dateOfBirth,
                gender,
                bloodType,
                height,
                weight,
                relationship,
                [],
                []);

            var result = await mediator.Send(command, cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to create family member profile.");

            return new ToolResult(true,
                $"Profile for {p.GetString("firstName")} created successfully.",
                result.Data,
                EntityType: "FamilyMember");
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
    }
}
