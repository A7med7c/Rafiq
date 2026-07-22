using MediatR;
using Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class DeleteMedicationTool(ISender mediator) : IVoiceTool
{
    public string Name => "delete_medication";
    public string Description => "Permanently deletes a medication and all its reminders.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — FIND the medication.\n" +
        "    Call list_medications.\n" +
        "    Filter by medicineName or keywords the user mentioned.\n" +
        "    ▸ No match → tell user: 'I couldn\\'t find a matching medication to delete.'\n" +
        "      Do not call delete_medication.\n" +
        "    ▸ Multiple matches → list them and ask which one:\n" +
        "      '1. Panadol 500 mg\\n" +
        "       2. Panadol 1000 mg\\n" +
        "       Which one would you like to delete?'\n" +
        "    ▸ Exactly one match → go to STEP 2.\n\n" +

        "  STEP 2 — CONFIRM (always, before deleting).\n" +
        "    'Are you sure you want to delete [name] ([dosage])? This will also permanently delete all its reminders.'\n" +
        "    Only call delete_medication after explicit yes.\n\n" +

        "  STEP 3 — CALL delete_medication.\n\n" +

        "REQUIRED:\n" +
        "  id: UUID from list_medications result.\n\n" +

        "SIDE EFFECTS:\n" +
        "  All reminders for this medication are permanently deleted. This cannot be undone.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("id"), out var id))
            return new ToolResult(false, "Invalid medication id.");

        try
        {
            var result = await mediator.Send(new DeleteUserMedicineCommand(id), cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to delete medication.");

            return new ToolResult(true, "Medication and all its reminders have been deleted.");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
