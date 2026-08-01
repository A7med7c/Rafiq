using MediatR;
using Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class AddMedicationTool(ISender mediator) : IVoiceTool
{
    public string Name => "add_medication";
    public string Description => "Adds a new medication to the user's health profile.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"medicineName\",\"frequency\",\"duration\"]," +
        "\"properties\":{" +
        "\"medicineName\":{\"type\":\"string\"}," +
        "\"dosage\":{\"type\":\"string\",\"description\":\"OPTIONAL — omit if the user doesn't know it\"}," +
        "\"frequency\":{\"type\":\"string\"}," +
        "\"duration\":{\"type\":\"string\"}," +
        "\"notes\":{\"type\":\"string\"}," +
        "\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: family member profileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => ["list_allergies", "add_medication_reminder"];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — COLLECT the REQUIRED fields (medicineName, frequency, duration) one at\n" +
        "    a time if not already given. Keep asking until each required field is confirmed.\n" +
        "    Make the conversation feel natural, not like filling a form.\n" +
        "    ⚠ Dosage is OPTIONAL — DO NOT require it. Many users don't know their dosage.\n" +
        "      Ask about dosage ONCE. If user says they don't know / skips / ignores → omit it.\n" +
        "    Never call add_medication until the three required fields are confirmed.\n\n" +

        "  STEP 2 — OPTIONAL notes (TWO-ATTEMPT RULE from the main prompt).\n" +
        "    First ask: EN: 'Any special instructions, like take with food or avoid sunlight?'\n" +
        "               AR: 'هل هناك تعليمات خاصة، مثل تناوله مع الطعام أو تجنب الشمس؟'\n" +
        "    If the user ignores it, ask ONE more brief time. Still no answer → omit and continue.\n" +
        "    If the user says 'No' / 'لا' / 'مش مهم' / 'بدون' → omit and never re-ask.\n\n" +

        "  STEP 3 — PHARMACOLOGICAL ALLERGY SAFETY CHECK — reason, don't just string-match.\n" +
        "    Call list_allergies FIRST (pass targetProfileId if this is for a family member).\n" +
        "    Do NOT rely on exact-name matching. Instead reason as follows:\n" +
        "      • What DRUG CLASS or ACTIVE INGREDIENT does the requested medication belong to?\n" +
        "        Examples of well-known conflicts you MUST catch:\n" +
        "          – Penicillin allergy → BLOCK amoxicillin, ampicillin, augmentin, flucloxacillin,\n" +
        "            piperacillin, and any '-cillin' or beta-lactam antibiotic.\n" +
        "          – Sulfa/sulfonamide allergy → BLOCK cotrimoxazole (Bactrim/Septrin),\n" +
        "            sulfamethoxazole, sulfasalazine.\n" +
        "          – NSAID / aspirin allergy → BLOCK ibuprofen, naproxen, diclofenac, brufen,\n" +
        "            voltaren, cataflam, and any other NSAID.\n" +
        "          – Paracetamol/acetaminophen allergy → BLOCK panadol, tylenol, adol, fevadol.\n" +
        "          – Codeine / opioid allergy → BLOCK tramadol, morphine, oxycodone, fentanyl.\n" +
        "          – Iodine / shellfish allergy → warn on contrast agents.\n" +
        "          – Statin allergy → BLOCK atorvastatin, simvastatin, rosuvastatin.\n" +
        "      • Also flag if the recorded allergen NAME appears as a substring or brand of the\n" +
        "        requested medication (e.g. allergy to 'Augmentin' → block 'Amoxicillin/Clavulanate').\n" +
        "      • Consider Arabic and English drug names; users may write either.\n" +
        "    If ANY plausible conflict is detected (name match, class match, ingredient overlap):\n" +
        "      STOP and warn:\n" +
        "        EN: '[Name] has a recorded allergy to [allergen] ([severity]). [Medication] is\n" +
        "             [in that class / contains that ingredient / may cross-react]. Adding it\n" +
        "             could be unsafe. Are you sure you want to proceed anyway?'\n" +
        "        AR: '[الاسم] عنده/عندها حساسية مسجلة من [المادة] ([الشدة]). [الدواء]\n" +
        "             [ينتمي لنفس الفئة / يحتوي على نفس المادة / قد يسبب تفاعل تحسسي].\n" +
        "             إضافته قد تكون غير آمنة. هل أنت متأكد من المتابعة؟'\n" +
        "      Only continue to STEP 4 after the user EXPLICITLY confirms.\n" +
        "    If no plausible conflict exists, continue immediately to STEP 4.\n\n" +

        "  STEP 4 — CALL add_medication and WAIT for the tool result.\n" +
        "    Only claim success AFTER the tool returns success=true.\n" +
        "    If success=false, relay the error — never fabricate 'تم الحفظ' or 'added'.\n\n" +

        "  STEP 5 — OFFER a reminder (ONLY AFTER a successful add).\n" +
        "    EN: 'Would you like me to set up a reminder for [medicineName]?'\n" +
        "    AR: 'هل تريد إنشاء تذكير لدواء [medicineName]؟'\n" +
        "    If yes → immediately start the add_medication_reminder workflow.\n" +
        "             Pass the id from the add_medication result as userMedicineId.\n" +
        "    If no  → finish the conversation naturally.\n\n" +

        "REQUIRED — collect all three before calling add_medication:\n" +
        "  medicineName: Name of the medication (e.g. 'Paracetamol'). Max 300 chars.\n" +
        "  frequency:    How often (e.g. 'twice a day', 'morning and evening').\n" +
        "                Keep the user's phrasing verbatim. Max 200 chars.\n" +
        "  duration:     How long (e.g. '7 days', '1 month', 'ongoing'). Max 200 chars.\n\n" +

        "OPTIONAL — ask at most once, skip if user doesn't provide:\n" +
        "  dosage: Amount per dose (e.g. '500 mg', '1 tablet'). Omit if user doesn't know.\n" +
        "  notes:  Special instructions. Max 1000 chars.\n\n" +

        "FAMILY MEMBER CONTEXT: If for a family member, pass their profileId as targetProfileId.\n" +
        "  Also pass targetProfileId to list_allergies so you check THEIR allergies, not yours.\n\n" +

        "BACKEND ENFORCES (relay any error returned; do not pre-validate):\n" +
        "  - Same name + dosage cannot already exist in this profile.\n" +
        "  - On duplicate error, suggest update_medication instead.\n\n" +

        "Result data includes the created medication id — pass it to add_medication_reminder.\n\n" +

        "LANGUAGE: Always respond in the same language the user is using (Arabic or English).";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        var medicineName = p.GetString("medicineName") ?? string.Empty;
        // Dosage is optional — stored as empty string when the user doesn't know it.
        var dosage       = p.GetString("dosage")       ?? string.Empty;
        var frequency    = p.GetString("frequency")    ?? string.Empty;
        var duration     = p.GetString("duration")     ?? string.Empty;
        var notes        = p.TryGetProperty("notes", out var n) ? n.GetString() : null;

        try
        {
            var result = await mediator.Send(
                new AddUserMedicineCommand(context.ProfileId, medicineName, dosage, frequency, duration,
                    notes, ImagePath: null, MedicineSource.Manual),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to add medication.");

            return new ToolResult(true,
                $"Medication '{medicineName}' added successfully.",
                result.Data,
                EntityType: "Medication");
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "DUPLICATE_MEDICATION");
        }
    }
}
