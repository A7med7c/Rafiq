using MediatR;
using Rafiq.Application.Features.MedicineReminders.Commands.UpdateMedicineReminder;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class UpdateMedicationReminderTool(ISender mediator) : IVoiceTool
{
    public string Name => "update_medication_reminder";
    public string Description => "Updates an existing medication reminder's time, dates, or repeat type.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\",\"reminderTime\",\"startDate\",\"endDate\",\"repeatType\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"reminderTime\":{\"type\":\"string\",\"description\":\"HH:mm (24-hour)\"}," +
        "\"startDate\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"endDate\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"repeatType\":{\"type\":\"string\",\"enum\":[\"Once\",\"Daily\",\"Weekly\",\"Monthly\"]}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps:\n\n" +

        "  STEP 1 — IDENTIFY medication and reminder.\n" +
        "    If you don't have the reminder id yet:\n" +
        "    a. Call list_medications to find the medication id.\n" +
        "    b. Call list_medication_reminders with that id to get the reminder(s).\n" +
        "    ▸ No reminders → tell user: 'This medication has no reminders set up.'\n" +
        "    ▸ Multiple reminders → list them and ask which one to update:\n" +
        "      '1. Daily at 8:00 AM (starts Jan 1)\n" +
        "       2. Daily at 9:00 PM (starts Jan 1)\n" +
        "       Which one would you like to update?'\n" +
        "    ▸ One reminder → confirm current values and proceed.\n\n" +

        "  STEP 2 — ASK what to change.\n" +
        "    Example: 'What would you like to change — the time, start date, end date, or repeat frequency?'\n\n" +

        "  STEP 3 — COLLECT new values; keep current values for unchanged fields.\n" +
        "    Apply DATE/TIME RESOLUTION and NATURAL LANGUAGE CONVERSION below.\n\n" +

        "  STEP 4 — CALL update_medication_reminder.\n\n" +

        "REQUIRED (pass current value for any field user did not change):\n" +
        "  id:           UUID of the reminder (from list_medication_reminders).\n" +
        "  reminderTime: HH:mm in 24-hour format (e.g. '08:00', '21:00').\n" +
        "                Convert natural language: '8 AM'→'08:00', '9 PM'→'21:00', 'morning'→'08:00'.\n" +
        "  startDate:    YYYY-MM-DD. Apply DATE/TIME RESOLUTION.\n" +
        "  endDate:      YYYY-MM-DD. Must be >= startDate. '2099-12-31' for ongoing.\n" +
        "  repeatType:   Once, Daily, Weekly, Monthly.\n\n" +

        "BACKEND ENFORCES (relay any error returned):\n" +
        "  - startDate cannot be before today when modifying it.\n" +
        "  - endDate must be >= startDate; for Once, endDate must equal startDate.\n" +
        "  - Duplicate reminder not allowed.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;

        if (!Guid.TryParse(p.GetString("id"), out var id))
            return new ToolResult(false, "Invalid reminder id.");

        var timeStr = p.GetString("reminderTime") ?? string.Empty;
        if (!TimeSpan.TryParse(timeStr, out var reminderTime))
            return new ToolResult(false, "Invalid reminderTime. Use HH:mm format (e.g. '08:00', '21:00').");

        if (!DateOnly.TryParse(p.GetString("startDate"), out var startDate))
            return new ToolResult(false, "Invalid startDate. Use YYYY-MM-DD format.");

        if (!DateOnly.TryParse(p.GetString("endDate"), out var endDate))
            return new ToolResult(false, "Invalid endDate. Use YYYY-MM-DD format.");

        if (!Enum.TryParse<RepeatType>(p.GetString("repeatType"), ignoreCase: true, out var repeatType))
            return new ToolResult(false, "Invalid repeatType. Must be Once, Daily, Weekly, or Monthly.");

        try
        {
            var result = await mediator.Send(
                new UpdateMedicineReminderCommand(id, reminderTime, startDate, endDate, repeatType),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to update reminder.");

            return new ToolResult(true, "Reminder updated successfully.", result.Data);
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
