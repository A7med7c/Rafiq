using MediatR;
using Rafiq.Application.Features.MedicineReminders.Commands.DeleteMedicineReminder;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class DeleteMedicationReminderTool(ISender mediator) : IVoiceTool
{
    public string Name => "delete_medication_reminder";
    public string Description => "Permanently deletes a single medication reminder.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps:\n\n" +

        "  STEP 1 — FIND the reminder.\n" +
        "    Call list_medications to find the medication id.\n" +
        "    Call list_medication_reminders with that id.\n" +
        "    ▸ No reminders → tell user: 'This medication has no reminders to delete.'\n" +
        "    ▸ Multiple reminders → list them and ask which one to delete.\n" +
        "    ▸ One reminder → go to STEP 2.\n\n" +

        "  STEP 2 — CONFIRM (always, before deleting).\n" +
        "    'Are you sure you want to delete the [time] reminder for [medicineName]?'\n" +
        "    Only call delete_medication_reminder after explicit yes.\n\n" +

        "  STEP 3 — CALL delete_medication_reminder.\n\n" +

        "REQUIRED:\n" +
        "  id: UUID of the reminder (from list_medication_reminders).\n\n" +

        "NOTE: To delete ALL reminders for a medication, call delete_medication_reminder once per reminder,\n" +
        "or use delete_medication if the user wants to remove the medication entirely.\n" +
        "To disable (not delete) a reminder, use toggle_medication_reminder instead.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("id"), out var id))
            return new ToolResult(false, "Invalid reminder id.");

        try
        {
            var result = await mediator.Send(new DeleteMedicineReminderCommand(id), cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to delete reminder.");

            return new ToolResult(true, "Reminder deleted successfully.");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
