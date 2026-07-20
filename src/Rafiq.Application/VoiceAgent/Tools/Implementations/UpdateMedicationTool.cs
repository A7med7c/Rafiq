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
        "\"required\":[\"id\",\"medicineName\",\"dosage\",\"frequency\",\"duration\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"medicineName\":{\"type\":\"string\"}," +
        "\"dosage\":{\"type\":\"string\"}," +
        "\"frequency\":{\"type\":\"string\"}," +
        "\"duration\":{\"type\":\"string\"}," +
        "\"notes\":{\"type\":\"string\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — IDENTIFY the medication.\n" +
        "    Call list_medications.\n" +
        "    Filter by medicineName or keywords the user mentioned.\n" +
        "    ▸ No match → tell user: 'I couldn\\'t find a matching medication.'\n" +
        "    ▸ Multiple matches → list them and ask which one:\n" +
        "      '1. Panadol 500 mg\\n" +
        "       2. Panadol 1000 mg\\n" +
        "       Which one would you like to update?'\n" +
        "    ▸ Exactly one match → confirm once: 'Found [name] [dosage]. Is that the one?'\n\n" +

        "  STEP 2 — ASK what to change.\n" +
        "    Example: 'What would you like to change — the dosage, frequency, duration, or notes?'\n\n" +

        "  STEP 3 — COLLECT new values; keep current values for unchanged fields.\n\n" +

        "  STEP 4 — CALL update_medication.\n\n" +

        "REQUIRED (pass current value for any field the user did not change):\n" +
        "  id:           UUID of the medication (from list_medications).\n" +
        "  medicineName: Name. Max 300 chars.\n" +
        "  dosage:       Dosage. Max 200 chars.\n" +
        "  frequency:    Frequency. Max 200 chars.\n" +
        "  duration:     Duration. Max 200 chars.\n\n" +

        "OPTIONAL (keep existing if not mentioned):\n" +
        "  notes: Special instructions.\n\n" +

        "BACKEND ENFORCES (relay any error returned):\n" +
        "  - Updated name+dosage must not duplicate another existing medication.\n" +
        "  - ImagePath and Source cannot be changed via this tool.";

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
