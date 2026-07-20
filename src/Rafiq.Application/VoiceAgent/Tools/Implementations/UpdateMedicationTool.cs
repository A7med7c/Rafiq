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
        "BEFORE CALLING — you need the medication id. If you don't have it yet:\n" +
        "  1. Call list_medications to get the list.\n" +
        "  2. Identify which medication the user means.\n" +
        "  3. Use its id here.\n\n" +
        "REQUIRED:\n" +
        "  id:           UUID of the medication to update (from list_medications result).\n" +
        "  medicineName: Updated name (max 300 chars).\n" +
        "  dosage:       Updated dosage (max 200 chars).\n" +
        "  frequency:    Updated frequency (max 200 chars).\n" +
        "  duration:     Updated duration (max 200 chars).\n\n" +
        "OPTIONAL:\n" +
        "  notes: Updated special instructions.\n\n" +
        "VALIDATION RULES:\n" +
        "  - Updated name+dosage combination must not duplicate another existing medication.\n" +
        "  - ImagePath and Source cannot be changed via this tool.\n\n" +
        "CONVERSATION HINT:\n" +
        "  Ask the user which fields they want to change. For unchanged fields, use the current values from list_medications.";

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
