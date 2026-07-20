using MediatR;
using Rafiq.Application.Features.MedicineReminders.Commands.ToggleMedicineReminderStatus;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ToggleMedicationReminderTool(ISender mediator) : IVoiceTool
{
    public string Name => "toggle_medication_reminder";
    public string Description => "Enables or disables a medication reminder without deleting it.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when the user says:\n" +
        "  'Turn off my [medicine] reminder'\n" +
        "  'Enable my [medicine] reminders again'\n" +
        "  'Pause reminders for [medicine]'\n" +
        "  'Resume reminders'\n\n" +

        "PREFER this over delete_medication_reminder when the user wants to temporarily disable.\n" +
        "Use delete_medication_reminder only when the user explicitly wants to permanently remove.\n\n" +

        "WORKFLOW:\n\n" +

        "  STEP 1 — FIND the reminder.\n" +
        "    Call list_medications to find the medication id.\n" +
        "    Call list_medication_reminders with that id.\n" +
        "    The result includes isEnabled — tell the user the current state.\n" +
        "    ▸ No reminders → tell user: 'This medication has no reminders.'\n" +
        "    ▸ Multiple reminders → list them; if user says 'all', toggle each one separately.\n" +
        "    ▸ One reminder → go to STEP 2.\n\n" +

        "  STEP 2 — CALL toggle_medication_reminder.\n" +
        "    The backend toggles the current state (enabled → disabled, disabled → enabled).\n" +
        "    After success, tell the user the new state.\n\n" +

        "REQUIRED:\n" +
        "  id: UUID of the reminder (from list_medication_reminders).";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("id"), out var id))
            return new ToolResult(false, "Invalid reminder id.");

        try
        {
            var result = await mediator.Send(
                new ToggleMedicineReminderStatusCommand(id), cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to toggle reminder.");

            return new ToolResult(true, "Reminder status toggled successfully.");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
