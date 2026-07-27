using MediatR;
using Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class UpdateMedicationTool(ISender mediator) : IVoiceTool
{
    public string Name => "update_medication";
    public string Description => "Updates an existing medication's details.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\",\"medicineName\",\"frequency\",\"duration\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"medicineName\":{\"type\":\"string\"}," +
        "\"dosage\":{\"type\":\"string\",\"description\":\"OPTIONAL — omit to keep empty; matches AddMedication behavior\"}," +
        "\"frequency\":{\"type\":\"string\"}," +
        "\"duration\":{\"type\":\"string\"}," +
        "\"notes\":{\"type\":\"string\"}," +
        "\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: family member profileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — IDENTIFY the medication.\n" +
        "    Call list_medications (pass targetProfileId if for a family member).\n" +
        "    Filter by medicineName or keywords the user mentioned.\n" +
        "    ▸ No match → tell the user exactly:\n" +
        "        EN: 'I couldn\\'t find a matching medication. Would you like me to list all your medications?'\n" +
        "        AR: 'لم أجد دواءً مطابقاً. هل تريد أن أعرض قائمة جميع أدويتك؟'\n" +
        "      If user says yes → call list_medications again and display the full list, then re-ask.\n" +
        "    ▸ Multiple matches → list them and ask which one:\n" +
        "        EN: '1. Panadol 500 mg\\n2. Panadol 1000 mg\\nWhich one would you like to update?'\n" +
        "        AR: '1. بانادول 500 مجم\\n2. بانادول 1000 مجم\\nأيهم تريد تعديله؟'\n" +
        "    ▸ Exactly one match → confirm: 'Found [name] [dosage]. Is that the one?'\n" +
        "    Keep asking until the user confirms the correct medication.\n\n" +

        "  STEP 2 — ASK what to change (REQUIRED FIELDS — KEEP ASKING).\n" +
        "    Ask: 'What would you like to change — the name, frequency, duration, dosage, or notes?'\n" +
        "    AR:  'ماذا تريد تغييره — الاسم، أو التكرار، أو المدة، أو الجرعة، أو الملاحظات؟'\n" +
        "    For each required field (medicineName, frequency, duration) the user wants to change:\n" +
        "      Keep re-asking with natural phrasing until the user provides it.\n" +
        "      Never skip a required field or assume a value.\n\n" +

        "  STEP 3 — COLLECT new values. For unchanged fields, pass the current value.\n" +
        "    OPTIONAL FIELDS — TWO-ATTEMPT RULE:\n" +
        "      dosage: If user hasn\\'t mentioned dosage, ask once:\n" +
        "        EN: 'Would you like to update the dosage?'\n" +
        "        AR: 'هل تريد تحديث الجرعة؟'\n" +
        "        If ignored → ask once more briefly. Still no answer → keep current value.\n" +
        "      notes: If user hasn\\'t mentioned notes, ask once:\n" +
        "        EN: 'Any updated instructions (like take with food)?'\n" +
        "        AR: 'هل هناك تعليمات محدثة (مثل تناوله مع الطعام)؟'\n" +
        "        If ignored → ask once more briefly. Still no answer → keep current value.\n" +
        "        If user says 'No' / 'لا' / 'مش مهم' → omit and never re-ask.\n\n" +

        "  STEP 4 — CALL update_medication and WAIT for the tool result.\n" +
        "    Only claim success AFTER the tool returns success=true.\n" +
        "    If success=false, relay the exact error message — never fabricate 'تم التعديل' or 'updated'.\n" +
        "    VALIDATION ERROR RECOVERY:\n" +
        "      • 'NOT_FOUND': The medication no longer exists. Tell the user and offer to list medications.\n" +
        "      • 'DUPLICATE_MEDICATION': The new name+dosage matches another medication.\n" +
        "          Suggest: 'You already have a medication with that name. Would you like to use a\n" +
        "          different dosage, or update that existing medication instead?'\n\n" +

        "REQUIRED (pass current value for any field the user did not change):\n" +
        "  id:           UUID of the medication (from list_medications).\n" +
        "  medicineName: Name. Max 300 chars.\n" +
        "  frequency:    How often. Max 200 chars.\n" +
        "  duration:     How long. Max 200 chars.\n\n" +

        "OPTIONAL (two-attempt rule; keep existing value if user doesn't answer):\n" +
        "  dosage: Dosage amount. Max 200 chars. Omit or pass empty string to clear it.\n" +
        "  notes:  Special instructions.\n" +
        "  targetProfileId: Family member's profileId from list_family_profiles.\n\n" +

        "PARTIAL SUCCESS: Report this operation's outcome on its own — do not bundle it with other\n" +
        "  operations in the same session. If updating the medication succeeds but a follow-on\n" +
        "  action (e.g., updating a reminder) fails, report each result separately and honestly.\n\n" +

        "BACKEND ENFORCES (relay any error returned; do not pre-validate):\n" +
        "  - Updated name+dosage must not duplicate another existing medication.\n" +
        "  - ImagePath and Source cannot be changed via this tool.\n\n" +

        "LANGUAGE: Always respond in the same language the user is using (Arabic or English).";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        if (!Guid.TryParse(p.GetString("id"), out var id))
            return new ToolResult(false, "Invalid medication id.");

        try
        {
            var result = await mediator.Send(
                new UpdateUserMedicineCommand(
                    id,
                    p.GetString("medicineName") ?? string.Empty,
                    p.GetString("dosage")       ?? string.Empty,
                    p.GetString("frequency")    ?? string.Empty,
                    p.GetString("duration")     ?? string.Empty,
                    p.TryGetProperty("notes", out var n) ? n.GetString() : null),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to update medication.");

            return new ToolResult(true, "Medication updated successfully.", result.Data);
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "DUPLICATE_MEDICATION");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
