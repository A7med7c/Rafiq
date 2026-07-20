using MediatR;
using Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminders;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListMedicationRemindersTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_medication_reminders";
    public string Description => "Returns all reminders for a specific medication.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"userMedicineId\"]," +
        "\"properties\":{" +
        "\"userMedicineId\":{\"type\":\"string\",\"format\":\"uuid\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL in these situations:\n" +
        "  • User asks to see, update, delete, or toggle reminders for a specific medication.\n" +
        "  • BEFORE update_medication_reminder / delete_medication_reminder / toggle_medication_reminder\n" +
        "    to find the correct reminder id.\n\n" +
        "REQUIRED:\n" +
        "  userMedicineId: UUID of the medication (from list_medications result).\n\n" +
        "Result fields per reminder: id, reminderTime (HH:mm:ss), startDate (YYYY-MM-DD),\n" +
        "  endDate (YYYY-MM-DD), repeatType (Once/Daily/Weekly/Monthly), isEnabled.\n" +
        "When showing to the user, format times naturally (e.g. '8:00 AM') and skip fields like id.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("userMedicineId"), out var medicineId))
            return new ToolResult(false, "Invalid medication id.");

        try
        {
            var result = await mediator.Send(
                new GetMedicineRemindersQuery(medicineId), cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to retrieve reminders.");

            var reminders = result.Data ?? [];
            return new ToolResult(true,
                reminders.Count == 0
                    ? "No reminders found for this medication."
                    : $"Found {reminders.Count} reminder(s).",
                new { reminders });
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
