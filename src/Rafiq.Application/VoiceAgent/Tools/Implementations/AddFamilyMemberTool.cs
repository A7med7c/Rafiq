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
        "USE THIS TOOL to add a family member who does NOT have their own Rafiq account.\n\n" +

        "═══════════════════════════ WORKFLOW ═══════════════════════════\n\n" +

        "STEP 1 — EXTRACT what the user already gave you.\n" +
        "  Users often pack several fields into one sentence:\n" +
        "    'Add my 10-year-old son Mohamed Ali'  → firstName=Mohamed, lastName=Ali,\n" +
        "       relationship=Son, gender=Male (implied), age≈10 (infer DOB).\n" +
        "    'عايز أضيف بنتي نور، عندها 7 سنين'    → firstName=نور, relationship=Daughter,\n" +
        "       gender=Female (implied), age≈7 (infer DOB).\n" +
        "  Extract everything you can. Ask ONLY for what is genuinely missing.\n\n" +

        "STEP 2 — COLLECT the five required fields (ask for missing ones naturally).\n" +
        "  Combine questions where sensible: 'What is her last name and date of birth?'\n" +
        "  Never ask for a field the user already provided.\n\n" +

        "STEP 3 — OFFER optional medical details (TWO-ATTEMPT RULE per field):\n" +
        "  Ask once each (combine into one question if possible):\n" +
        "    EN: 'Do you know their blood type, height, or weight? (you can skip any of these)'\n" +
        "    AR: 'هل تعرف فصيلة الدم أو الطول أو الوزن؟ (ممكن تتجاوزها)'\n" +
        "  If the user says no / skip / لا / تجاوز / مش عارف → skip ALL optional fields.\n" +
        "  Do NOT ask separately about allergies or chronic diseases — add those after creation.\n\n" +

        "STEP 4 — CONFIRM before creating (always use ask_user):\n" +
        "  Show a clear summary in the user's language, then ask to proceed.\n" +
        "  EN:\n" +
        "    'I'll create a health profile for:\n" +
        "     • Name: [firstName] [lastName]\n" +
        "     • Relationship: [relationship]\n" +
        "     • Date of birth: [dateOfBirth]\n" +
        "     • Gender: [Male/Female]\n" +
        "     [• Blood type: X  (only if provided)]\n" +
        "     [• Height: X cm / Weight: X kg  (only if provided)]\n" +
        "     Shall I go ahead?'\n" +
        "  AR:\n" +
        "    'سأنشئ ملف صحي لـ:\n" +
        "     • الاسم: [firstName] [lastName]\n" +
        "     • الصلة: [relationship بالعربي]\n" +
        "     • تاريخ الميلاد: [dateOfBirth]\n" +
        "     • الجنس: [ذكر/أنثى]\n" +
        "     هل تريد المتابعة؟'\n\n" +

        "STEP 5 — CALL add_family_member only after explicit confirmation.\n" +
        "  On success, offer to continue: add medications, book an appointment, etc.\n\n" +

        "════════════════════════ FIELD REFERENCE ════════════════════════\n\n" +

        "REQUIRED — never invent, never guess:\n" +
        "  firstName   — given name. Max 100 chars.\n" +
        "  lastName    — family/last name. Max 100 chars.\n" +
        "  dateOfBirth — YYYY-MM-DD. Must be in the past.\n" +
        "                Age → DOB: subtract from today's year. Use Jan 1 if no month given.\n" +
        "                  'he is 10'       → TODAY_YEAR - 10 + '-01-01' (confirm before saving)\n" +
        "                  'born in 2012'   → '2012-01-01'\n" +
        "                  'born March 2015'→ '2015-03-01'\n" +
        "                Always tell the user the computed date and ask to confirm.\n" +
        "  gender      — 'Male' or 'Female'.\n" +
        "                Infer silently from relationship when unambiguous (confirm in summary):\n" +
        "                  Male  → Son, Father, Husband, Brother, Grandfather\n" +
        "                  Female→ Daughter, Mother, Wife, Sister, Grandmother\n" +
        "                For Other/ambiguous relationships, ask explicitly.\n" +
        "  relationship— enum value to send (always English regardless of conversation language):\n" +
        "                  Son | Daughter | Father | Mother | Husband | Wife |\n" +
        "                  Brother | Sister | Grandfather | Grandmother | Other\n" +
        "                Arabic → enum mapping:\n" +
        "                  ابن/ولد           → Son\n" +
        "                  بنت/ابنة          → Daughter\n" +
        "                  أب/والد           → Father\n" +
        "                  أم/والدة          → Mother\n" +
        "                  زوج              → Husband\n" +
        "                  زوجة             → Wife\n" +
        "                  أخ              → Brother\n" +
        "                  أخت             → Sister\n" +
        "                  جد/جدو           → Grandfather\n" +
        "                  جدة/ستو          → Grandmother\n" +
        "                  عم/خال/ابن عم/غيره→ Other\n\n" +

        "OPTIONAL — ask at most once; omit if user skips:\n" +
        "  bloodType — APositive | ANegative | BPositive | BNegative |\n" +
        "              ABPositive | ABNegative | OPositive | ONegative\n" +
        "              Arabic input: A+ → APositive, B- → BNegative, O+ → OPositive, etc.\n" +
        "  height    — centimetres (30–300).\n" +
        "  weight    — kilograms (1–500).\n\n" +

        "BACKEND ENFORCES (never pre-validate; relay errors as-is):\n" +
        "  dateOfBirth cannot be in the future · height 30–300 · weight 1–500 · no Self.\n\n" +

        "AFTER SUCCESS:\n" +
        "  Confirm the name and offer what to do next:\n" +
        "    EN: '[Name]'s profile has been created! Would you like to add their medications,\n" +
        "        book an appointment, or do something else for them?'\n" +
        "    AR: 'تم إنشاء ملف [Name] بنجاح! هل تريد إضافة أدويته/أدويتها أو حجز موعد؟'";

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
