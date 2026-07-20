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
        "REQUIRED — never invent these; ask the user for each one explicitly:\n" +
        "  medicineName: Name of the medication (e.g. 'Paracetamol', 'Amoxicillin'). Max 300 chars.\n" +
        "  dosage:       Amount per dose (e.g. '500mg', '1 tablet', '10ml'). Max 200 chars.\n" +
        "  frequency:    How often (e.g. 'twice daily', 'every 8 hours'). Max 200 chars.\n" +
        "  duration:     How long (e.g. '7 days', '1 month', 'ongoing'). Max 200 chars.\n\n" +
        "OPTIONAL — ask at most once; if user declines or says 'skip' / 'doesn't matter' → omit:\n" +
        "  notes: Special instructions (e.g. 'take with food'). Max 1000 chars. Default: omit.\n\n" +
        "BACKEND ENFORCES (do not pre-validate yourself — just call the tool; relay any error returned):\n" +
        "  - A medication with the same name AND dosage cannot already exist in this profile.\n" +
        "  - If the tool returns a duplicate error, suggest using update_medication instead.\n\n" +
        "The result data includes the created medication id — pass it to add_medication_reminder if needed.";

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
