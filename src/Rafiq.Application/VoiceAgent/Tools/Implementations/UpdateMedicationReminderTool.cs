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
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — IDENTIFY the medication.\n" +
        "    If you don't have the reminder id yet:\n" +
        "    a. Call list_medications (pass targetProfileId if for a family member) to find the medication.\n" +
        "       Filter by the name or keywords the user mentioned.\n" +
        "       ▸ No match → tell the user and offer to list all medications.\n" +
        "       ▸ Multiple matches → list them, ask which one.\n" +
        "       ▸ One match → confirm once, then proceed.\n" +
        "    b. Call list_medication_reminders with the medication id to get its reminders.\n" +
        "       ▸ No reminders → tell user: 'This medication has no reminders set up. Would you like to add one?'\n" +
        "         If yes → use the add_medication_reminder workflow instead.\n" +
        "       ▸ Multiple reminders → list them and ask which one to update:\n" +
        "           EN: '1. Daily at 8:00 AM (starts Jan 1)\\n2. Daily at 9:00 PM (starts Jan 1)\\nWhich one?'\n" +
        "           AR: '1. يومياً الساعة 8 صباحاً\\n2. يومياً الساعة 9 مساءً\\nأيهم تريد تعديله؟'\n" +
        "       ▸ One reminder → confirm current values: 'Found a [repeatType] reminder at [time].\n" +
        "           Is that the one you want to update?' Then proceed.\n\n" +

        "  STEP 2 — ASK what to change (REQUIRED FIELDS — KEEP ASKING).\n" +
        "    EN: 'What would you like to change — the time, start date, end date, or repeat frequency?'\n" +
        "    AR: 'ماذا تريد تغييره — الوقت، أو تاريخ البداية، أو تاريخ النهاية، أو نوع التكرار؟'\n" +
        "    For each field the user wants to change: keep asking with natural phrasing until provided.\n" +
        "    Never invent values or assume — always ask.\n\n" +

        "  STEP 3 — COLLECT new values; keep current values for unchanged fields.\n" +
        "    TIME CONVERSION:\n" +
        "      '8 AM' → '08:00', '9 PM' → '21:00', 'morning' → '08:00', 'evening' → '20:00'\n" +
        "      'الصبح' → '08:00', 'المساء' → '20:00', 'الظهر' → '12:00', 'الليل' → '22:00'\n" +
        "    DATE RESOLUTION: Apply relative dates from today's date context.\n" +
        "      'tomorrow' → today + 1 day. 'next Monday' → nearest upcoming Monday.\n" +
        "    VALIDATION BEFORE CALLING (pre-check only these):\n" +
        "      • reminderTime must be parseable as HH:mm\n" +
        "      • startDate and endDate must be YYYY-MM-DD\n" +
        "      • endDate must be >= startDate\n" +
        "      • For Once repeatType, endDate must equal startDate\n" +
        "      If any check fails → ask the user to correct it; do NOT call the tool with invalid values.\n\n" +

        "  STEP 4 — CALL update_medication_reminder and WAIT for the tool result.\n" +
        "    Only claim success AFTER the tool returns success=true.\n" +
        "    If success=false, relay the exact error — never fabricate 'تم التعديل' or 'updated'.\n" +
        "    VALIDATION ERROR RECOVERY:\n" +
        "      • 'NOT_FOUND': Reminder no longer exists. Tell user and offer to list reminders again.\n" +
        "      • 'VALIDATION_ERROR' / startDate in past: Ask for a future start date.\n" +
        "      • 'VALIDATION_ERROR' / duplicate reminder: Tell user a reminder at that time/date\n" +
        "          already exists and ask them to choose a different time.\n\n" +

        "REQUIRED (pass current value for any field user did not change):\n" +
        "  id:           UUID of the reminder (from list_medication_reminders).\n" +
        "  reminderTime: HH:mm 24-hour format (e.g. '08:00', '21:00').\n" +
        "  startDate:    YYYY-MM-DD.\n" +
        "  endDate:      YYYY-MM-DD. Must be >= startDate. Use '2099-12-31' for ongoing.\n" +
        "  repeatType:   Once, Daily, Weekly, Monthly.\n\n" +

        "PARTIAL SUCCESS: Report this operation's result independently — do not bundle it with other\n" +
        "  operations in the same session. If the medication update succeeded but this reminder\n" +
        "  update fails, say exactly that.\n\n" +

        "BACKEND ENFORCES (relay any error returned; do not pre-validate beyond the checks in STEP 3):\n" +
        "  - startDate cannot be before today.\n" +
        "  - endDate must be >= startDate; for Once, endDate must equal startDate.\n" +
        "  - Duplicate reminder (same time + dates + repeat) not allowed.\n\n" +

        "LANGUAGE: Always respond in the same language the user is using (Arabic or English).";

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
