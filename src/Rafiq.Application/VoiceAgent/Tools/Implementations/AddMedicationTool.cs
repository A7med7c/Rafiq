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
        "\"required\":[\"medicineName\",\"dosage\",\"frequency\",\"duration\"]," +
        "\"properties\":{" +
        "\"medicineName\":{\"type\":\"string\"}," +
        "\"dosage\":{\"type\":\"string\"}," +
        "\"frequency\":{\"type\":\"string\"}," +
        "\"duration\":{\"type\":\"string\"}," +
        "\"notes\":{\"type\":\"string\"}" +
        "}}";

    public string[] RelatedToolNames => ["add_medication_reminder"];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — COLLECT all four required fields one at a time if not already given.\n" +
        "    Make the conversation feel natural, not like filling a form.\n" +
        "    Never call any tool until all four are confirmed.\n\n" +

        "  STEP 2 — OPTIONAL notes (ask at most once).\n" +
        "    EN: 'Any special instructions, like take with food or avoid sunlight?'\n" +
        "    AR: 'هل هناك تعليمات خاصة، مثل تناوله مع الطعام أو تجنب الشمس؟'\n" +
        "    Skip notes immediately — without asking again — if the user says:\n" +
        "      'No' / 'Skip' / 'Not important' / 'Doesn\\'t matter' / 'None' / 'Nothing'\n" +
        "      / 'لا' / 'تجاوز' / 'مش مهم' / 'بدون' / 'لا يهم' / 'عادي'\n" +
        "      / or the user ignores the question and gives other information.\n" +
        "    Omit the notes field if skipped.\n\n" +

        "  STEP 3 — CALL add_medication.\n\n" +

        "  STEP 4 — OFFER a reminder (at most once, only after a successful add).\n" +
        "    EN: 'Would you like me to set up a reminder for [medicineName]?'\n" +
        "    AR: 'هل تريد إنشاء تذكير لدواء [medicineName]؟'\n" +
        "    If yes → immediately start the add_medication_reminder workflow.\n" +
        "             Pass the id from the add_medication result as userMedicineId.\n" +
        "             Keep the medicineName, dosage, frequency, duration in context for the summary.\n" +
        "    If no  → finish the conversation.\n\n" +

        "REQUIRED — collect all four before calling:\n" +
        "  medicineName: Name of the medication (e.g. 'Paracetamol'). Max 300 chars.\n" +
        "  dosage:       Amount per dose (e.g. '500 mg', '1 tablet', '10 ml'). Max 200 chars.\n" +
        "  frequency:    How often. Keep the user\\'s phrasing verbatim. Max 200 chars.\n" +
        "  duration:     How long (e.g. '7 days', '1 month', 'ongoing'). Max 200 chars.\n\n" +

        "OPTIONAL:\n" +
        "  notes: Special instructions. Max 1000 chars.\n\n" +

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
