using MediatR;
using Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class AddMedicationReminderTool(ISender mediator) : IVoiceTool
{
    public string Name => "add_medication_reminder";
    public string Description => "Sets up daily/weekly/one-time reminder notifications for a medication.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"userMedicineId\",\"startDate\",\"endDate\",\"repeatType\",\"times\"]," +
        "\"properties\":{" +
        "\"userMedicineId\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"startDate\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"endDate\":{\"type\":\"string\",\"format\":\"date\"}," +
        "\"repeatType\":{\"type\":\"string\",\"enum\":[\"Once\",\"Daily\",\"Weekly\",\"Monthly\"]}," +
        "\"times\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"minItems\":1}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — COLLECT all required fields.\n" +
        "    Ask for each one naturally if not already provided by the user.\n" +
        "    Ask all four before calling any tool.\n\n" +
        "    times:      One or more reminder times. See NATURAL LANGUAGE → CONCRETE VALUES.\n" +
        "    repeatType: How often. See NATURAL LANGUAGE → CONCRETE VALUES.\n" +
        "    startDate:  When to start. Apply DATE/TIME RESOLUTION. Must be today or later.\n" +
        "    endDate:    When to stop. Must be >= startDate.\n" +
        "                • 'same as medication duration' / 'for the whole course'\n" +
        "                  → compute startDate + medication duration from context.\n" +
        "                • 'ongoing' / 'indefinitely' / 'no end' / 'مستمر' / 'بدون نهاية'\n" +
        "                  → use 2099-12-31.\n" +
        "                • repeatType Once → endDate must equal startDate.\n\n" +

        "  STEP 2 — CONVERSATIONAL VALIDATION (before showing summary; do not call the tool yet).\n" +
        "    Check each condition below and handle it before proceeding:\n\n" +
        "    ▸ Reminder time already passed today AND startDate is today:\n" +
        "        EN: 'That time has already passed today. Would you like to start from tomorrow, or choose a different time?'\n" +
        "        AR: 'هذا الوقت مضى اليوم. هل تريد البدء من الغد، أم تختار وقتاً آخر؟'\n" +
        "        Do NOT silently change the time or date. Wait for the user to decide.\n\n" +
        "    ▸ endDate is before startDate:\n" +
        "        EN: 'The end date is before the start date. Please choose a later end date.'\n" +
        "        AR: 'تاريخ الانتهاء قبل تاريخ البداية. من فضلك اختر تاريخ انتهاء لاحقاً.'\n" +
        "        Wait for correction.\n\n" +
        "    ▸ Schedule phrasing is ambiguous and cannot be converted:\n" +
        "        EN: 'Could you clarify when you\\'d like the reminder? For example: every day at 8 PM.'\n" +
        "        AR: 'ممكن توضح متى تريد التذكير؟ مثلاً: كل يوم الساعة 8 مساءً.'\n\n" +

        "  STEP 3 — CONFIRMATION SUMMARY (always, before calling the tool).\n" +
        "    Never skip this step. Build the summary from conversation context.\n" +
        "    Show medication details (from the earlier add_medication or list_medications) and reminder details.\n\n" +
        "    English format:\n" +
        "      Medication:\n" +
        "      • [medicineName]\n" +
        "      • [dosage]\n" +
        "      • [frequency]\n" +
        "      • [duration]\n\n" +
        "      Reminder:\n" +
        "      • [repeatType in plain English, e.g. 'Every day']\n" +
        "      • [times as 12-hour, e.g. '8:00 PM']\n" +
        "      • Start: [startDate formatted naturally, e.g. '20 Jul 2026']\n" +
        "      • End: [endDate formatted naturally]\n\n" +
        "      Does everything look correct?\n\n" +
        "    Arabic format:\n" +
        "      الدواء:\n" +
        "      • [medicineName]\n" +
        "      • [dosage]\n" +
        "      • [frequency]\n" +
        "      • [duration]\n\n" +
        "      التذكير:\n" +
        "      • [repeatType بالعربي، مثلاً 'يومياً']\n" +
        "      • [الوقت بصيغة مفهومة، مثلاً '8:00 مساءً']\n" +
        "      • البداية: [startDate]\n" +
        "      • النهاية: [endDate]\n\n" +
        "      هل كل شيء صحيح؟\n\n" +
        "    If the user says yes / نعم / تمام / اوكي / correct / looks good → go to STEP 4.\n" +
        "    If the user corrects something → update that value and re-show the summary.\n\n" +

        "  STEP 4 — CALL add_medication_reminder.\n\n" +

        "REQUIRED:\n" +
        "  userMedicineId: UUID of the medication (from add_medication result or list_medications).\n" +
        "  startDate:      YYYY-MM-DD.\n" +
        "  endDate:        YYYY-MM-DD.\n" +
        "  repeatType:     Once | Daily | Weekly | Monthly.\n" +
        "  times:          Array of HH:mm (24-hour). At least one.\n\n" +

        "NATURAL LANGUAGE → CONCRETE VALUES:\n" +
        "  RepeatType:\n" +
        "    'every day' / 'daily' / 'كل يوم' / 'يومياً'                    → Daily\n" +
        "    'once' / 'just today' / 'مرة واحدة' / 'اليوم فقط'              → Once\n" +
        "    'every week' / 'weekly' / 'every Monday' / 'كل أسبوع'           → Weekly\n" +
        "    'every weekday' / 'weekdays only' / 'أيام العمل'                → Daily\n" +
        "    'every month' / 'monthly' / 'كل شهر' / 'شهرياً'                → Monthly\n\n" +
        "  Times (convert to HH:mm 24-hour):\n" +
        "    'morning'    / '8 AM'  / 'الصباح' / 'الفجر'   → ['08:00']\n" +
        "    'noon'       / '12 PM' / 'الظهر'              → ['12:00']\n" +
        "    'afternoon'  / '3 PM'  / 'العصر'              → ['15:00']\n" +
        "    'evening'    / '6 PM'  / 'المساء'             → ['18:00']\n" +
        "    'night'      / '9 PM'  / 'الليل'              → ['21:00']\n" +
        "    'every 6 hours'  / 'كل 6 ساعات'               → Daily, ['06:00','12:00','18:00','00:00']\n" +
        "    'every 8 hours'  / 'كل 8 ساعات'               → Daily, ['08:00','16:00','00:00']\n" +
        "    'every 12 hours' / 'كل 12 ساعة'               → Daily, ['08:00','20:00']\n" +
        "    'twice a day' / 'مرتين يومياً'                → Daily, ['08:00','18:00']\n" +
        "    'three times a day' / '3 مرات يومياً'         → Daily, ['08:00','14:00','20:00']\n" +
        "    'with meals'  / 'مع الوجبات'                  → Daily, ['08:00','13:00','19:00']\n" +
        "    'after meals' / 'بعد الأكل'                   → Daily, ['09:00','14:00','20:00']\n\n" +

        "BACKEND ENFORCES (relay any error returned; never pre-validate):\n" +
        "  - Duplicate reminder (same time + startDate + endDate + repeatType) is not allowed.\n" +
        "  - Times must be valid HH:mm strings.\n\n" +

        "Multiple reminders are created in one call by passing multiple times in the array.\n\n" +

        "LANGUAGE: Always respond in the same language the user is using (Arabic or English).\n" +
        "  Use the matching confirmation template above.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        if (!Guid.TryParse(p.GetString("userMedicineId"), out var medicineId))
            return new ToolResult(false, "Invalid medication id.");

        if (!DateOnly.TryParse(p.GetString("startDate"), out var startDate))
            return new ToolResult(false, "Invalid startDate. Use format YYYY-MM-DD.");

        if (!DateOnly.TryParse(p.GetString("endDate"), out var endDate))
            return new ToolResult(false, "Invalid endDate. Use format YYYY-MM-DD.");

        if (!Enum.TryParse<RepeatType>(p.GetString("repeatType"), ignoreCase: true, out var repeatType))
            return new ToolResult(false, "Invalid repeatType. Must be Once, Daily, Weekly, or Monthly.");

        var timesElement = p.GetProperty("times");
        var times = new List<string>();
        foreach (var t in timesElement.EnumerateArray())
        {
            var s = t.GetString();
            if (!string.IsNullOrWhiteSpace(s)) times.Add(s);
        }

        if (times.Count == 0)
            return new ToolResult(false, "At least one reminder time is required.");

        try
        {
            var result = await mediator.Send(
                new CreateMedicineRemindersCommand(medicineId, startDate, endDate, repeatType, times),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to create reminder.");

            var count = result.Data?.Count ?? 0;
            return new ToolResult(true, $"{count} reminder(s) set up successfully.", result.Data);
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
