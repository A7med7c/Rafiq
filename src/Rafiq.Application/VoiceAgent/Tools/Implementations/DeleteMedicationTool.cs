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
        "DESTRUCTIVE ACTION — always confirm with ask_user before calling:\n" +
        "  'Are you sure you want to delete [medicineName]? This will also delete all its reminders.'\n\n" +
        "BEFORE CALLING — you need the medication id. Call list_medications first if you don't have it.\n\n" +
        "REQUIRED:\n" +
        "  id: UUID of the medication to delete.\n\n" +
        "SIDE EFFECTS:\n" +
        "  All reminders for this medication are also deleted and their scheduled notifications cancelled.\n" +
        "  This action cannot be undone.";

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
