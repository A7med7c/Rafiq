using MediatR;
using Rafiq.Application.Features.PatientProfiles.Queries.GetAccessibleHealthProfiles;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListFamilyProfilesTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_family_profiles";
    public string Description => "Lists all health profiles accessible to the user: their own profile and any family members they manage or have been granted access to.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";
    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL whenever the user refers to a family member by relationship or name,\n" +
        "or before any operation that involves a family member's health data.\n\n" +

        "RESULT FIELDS per profile:\n" +
        "  userHealthProfileId  — UUID to pass as targetProfileId in other tool calls\n" +
        "  firstName, lastName  — the member's name\n" +
        "  dateOfBirth          — use to resolve 'oldest', 'youngest', 'the 8-year-old'\n" +
        "  relationship         — Son, Daughter, Father, Mother, Husband, Wife, Brother,\n" +
        "                         Sister, Grandfather, Grandmother, Other, Self\n" +
        "  accessRole           — Owner, Manager, or Viewer\n" +
        "  profileType          — 'Managed' (no account) or 'Shared' (has account)\n" +
        "  isSelf               — true when this is the authenticated user's own profile\n\n" +

        "IMPORTANT: isSelf == true means the authenticated user's OWN profile. NEVER use a\n" +
        "  profile where isSelf == true when answering questions about a FAMILY MEMBER.\n" +
        "  Only use isSelf == true profiles when the user is asking about themselves.\n\n" +

        "RELATIONSHIP RESOLUTION — map natural language to the relationship field:\n" +
        "  'my son'         → relationship == Son\n" +
        "  'my daughter'    → relationship == Daughter\n" +
        "  'my wife'        → relationship == Wife\n" +
        "  'my husband'     → relationship == Husband\n" +
        "  'my father'      → relationship == Father\n" +
        "  'my mother'      → relationship == Mother\n" +
        "  'my brother'     → relationship == Brother\n" +
        "  'my sister'      → relationship == Sister\n" +
        "  'my grandfather' → relationship == Grandfather\n" +
        "  'my grandmother' → relationship == Grandmother\n" +
        "  'ابني'           → Son | 'ابنتي' → Daughter | 'زوجتي' → Wife\n" +
        "  'زوجي'           → Husband | 'أبي' → Father | 'أمي' → Mother\n\n" +

        "PLURAL/GROUP RELATIONSHIPS — when the user asks about a group:\n" +
        "  'my children' / 'أولادي' / 'أبنائي' → all profiles where relationship is Son OR Daughter\n" +
        "  'my sons'                            → all profiles where relationship == Son\n" +
        "  'my daughters' / 'بناتي'             → all profiles where relationship == Daughter\n" +
        "  'my parents' / 'والديا'              → all profiles where relationship is Father OR Mother\n" +
        "  'my siblings' / 'إخواتي'             → all profiles where relationship is Brother OR Sister\n" +
        "  For group queries, call each matching profile's data tool with their respective\n" +
        "  targetProfileId and aggregate the results before answering.\n\n" +

        "DISAMBIGUATION — when multiple members match the same singular relationship:\n" +
        "  Never guess. List them numbered with name and age, then ask:\n" +
        "  EN: 'I found two sons: 1. Omar (born 2012), 2. Ali (born 2016). Which one?'\n" +
        "  AR: 'وجدت ابنين: 1. عمر (مواليد 2012)، 2. علي (مواليد 2016). أيهما تقصد؟'\n\n" +

        "CONTEXT MEMORY — once a family member is identified during this conversation:\n" +
        "  Pronouns and references ('him', 'her', 'that one', 'the same one', 'هو', 'هي')\n" +
        "  refer to the last identified member. Reuse their userHealthProfileId without\n" +
        "  calling this tool again unless the context clearly changes.\n\n" +

        "CROSS-PROFILE OPERATIONS — how to act on a family member's health data:\n" +
        "  After identifying the member, pass their userHealthProfileId as 'targetProfileId'\n" +
        "  in the parameters of any subsequent tool call. The system automatically routes\n" +
        "  the entire operation to that family member's profile.\n\n" +
        "  Examples:\n" +
        "    'Add aspirin for my son'\n" +
        "      → list_family_profiles → find son → son.userHealthProfileId = \"abc\"\n" +
        "      → add_medication { \"medicineName\":\"Aspirin\", ..., \"targetProfileId\":\"abc\" }\n\n" +
        "    'Show my father's appointments'\n" +
        "      → list_family_profiles → find father → targetProfileId = father's id\n" +
        "      → list_appointments { \"targetProfileId\":\"<uuid>\" }\n\n" +
        "    'Book a dentist appointment for my wife'\n" +
        "      → list_family_profiles → find wife → targetProfileId = wife's id\n" +
        "      → book_appointment { ..., \"targetProfileId\":\"<uuid>\" }\n\n" +
        "    'Update my daughter's medication reminder'\n" +
        "      → list_family_profiles → find daughter → targetProfileId = daughter's id\n" +
        "      → list_medications { \"targetProfileId\":\"<uuid>\" } → find medication\n" +
        "      → update_medication_reminder { ..., \"targetProfileId\":\"<uuid>\" }\n\n" +

        "PERMISSIONS — the accessRole field governs what is allowed:\n" +
        "  Owner / Manager → read and write (all operations)\n" +
        "  Viewer          → read only; write operations will be rejected by the backend\n" +
        "  Always attempt the operation; relay any permission error returned by the tool.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccessibleHealthProfilesQuery(), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve family profiles.");

        var profiles = result.Data ?? [];
        var familyCount = profiles.Count(p => !p.IsSelf);
        return new ToolResult(true,
            profiles.Count == 0
                ? "No accessible profiles found."
                : $"Found {profiles.Count} profile(s): {familyCount} family member(s).",
            new { profiles });
    }
}
