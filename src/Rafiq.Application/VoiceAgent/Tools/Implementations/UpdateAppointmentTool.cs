using MediatR;
using Rafiq.Application.Features.Appointments.Commands.UpdateAppointment;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class UpdateAppointmentTool(ISender mediator) : IVoiceTool
{
    public string Name => "update_appointment";
    public string Description => "Updates an existing appointment's details, reminder offset, or notes.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\",\"appointmentType\",\"title\",\"provider\",\"appointmentDateTime\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}," +
        "\"appointmentType\":{\"type\":\"string\"}," +
        "\"customType\":{\"type\":\"string\"}," +
        "\"title\":{\"type\":\"string\"}," +
        "\"provider\":{\"type\":\"string\"}," +
        "\"appointmentDateTime\":{\"type\":\"string\",\"format\":\"date-time\"}," +
        "\"reminderOffsetMinutes\":{\"type\":\"integer\",\"minimum\":0}," +
        "\"notes\":{\"type\":\"string\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — IDENTIFY the appointment.\n" +
        "    Call list_upcoming_appointments.\n" +
        "    Filter results using type, title, provider, date, or any keywords the user mentioned.\n" +
        "    ▸ No match → tell user:\n" +
        "        EN: 'I couldn\\'t find a matching upcoming appointment. Would you like me to list all?'\n" +
        "        AR: 'لم أجد موعداً مطابقاً. هل تريد أن أعرض جميع مواعيدك القادمة؟'\n" +
        "      If yes → display the full list, then re-ask which one to update.\n" +
        "    ▸ Exactly one match → confirm once: 'I found your [title] with [provider] on [date]\n" +
        "        at [time]. Is that the one you want to update?'\n" +
        "    ▸ Multiple matches → list them and ask which one:\n" +
        "        EN: '1. Dentist on Monday 21 July at 4:00 PM\\n2. Dentist on Friday 25 July at 10:00 AM\\nWhich one?'\n" +
        "        AR: '1. أسنان الاثنين 21 يوليو الساعة 4 مساءً\\n2. أسنان الجمعة 25 يوليو الساعة 10 صباحاً\\nأيهم؟'\n" +
        "    Keep asking until the user confirms the correct appointment.\n\n" +

        "  STEP 2 — ASK what to change (REQUIRED FIELDS — KEEP ASKING).\n" +
        "    EN: 'What would you like to update — the date, time, type, title, provider, or something else?'\n" +
        "    AR: 'ماذا تريد تعديله — التاريخ، أو الوقت، أو النوع، أو العنوان، أو المزود؟'\n" +
        "    For each required field the user wants to change: keep re-asking until provided.\n" +
        "    ⚠ If user changes date but NOT time → ALWAYS ask: 'What time on [new date]?'\n" +
        "      Never invent a time. Never keep an old time without confirming.\n\n" +

        "  STEP 3 — COLLECT new values; keep current values for unchanged fields.\n" +
        "    APPOINTMENT TYPE MAPPING (Arabic → enum):\n" +
        "      أسنان / سنان → Dentist\n" +
        "      تحليل / تحاليل → LabTest\n" +
        "      أشعة / صور → Imaging\n" +
        "      تطعيم / لقاح → Vaccination\n" +
        "      متابعة → FollowUp\n" +
        "      دكتور / طبيب / كشف → DoctorVisit\n" +
        "      نفسي / جلسة → Therapy\n" +
        "    DATE/TIME RESOLUTION:\n" +
        "      Convert relative dates: 'tomorrow' → today+1, 'next [day]' → nearest that weekday.\n" +
        "      Never generate appointmentDateTime in the past — if the computed time is past, re-ask.\n" +
        "    OPTIONAL FIELDS — TWO-ATTEMPT RULE:\n" +
        "      reminderOffsetMinutes: If user hasn\\'t mentioned a reminder, ask once:\n" +
        "        EN: 'Would you like to update the reminder? How many minutes before the appointment?'\n" +
        "        AR: 'هل تريد تحديث التذكير؟ كم دقيقة قبل الموعد؟'\n" +
        "        If ignored → ask once more briefly. Still no answer → keep current value.\n" +
        "        REMINDER VALIDATION: trigger = appointmentDateTime − reminderOffsetMinutes.\n" +
        "          If trigger is not in the future → warn and ask for a smaller offset.\n" +
        "      notes: If user hasn\\'t mentioned notes, ask once:\n" +
        "        EN: 'Any notes to update (like bring previous test results)?'\n" +
        "        AR: 'هل هناك ملاحظات تريد تحديثها (مثل إحضار نتائج سابقة)؟'\n" +
        "        If ignored → ask once more briefly. Still no answer → keep current value.\n" +
        "        If user says 'No' / 'لا' / 'مش مهم' → keep as-is and never re-ask.\n\n" +

        "  STEP 4 — CALL update_appointment and WAIT for the tool result.\n" +
        "    Only claim success AFTER the tool returns success=true.\n" +
        "    If success=false, relay the exact error — never fabricate 'تم التعديل' or 'updated'.\n" +
        "    VALIDATION ERROR RECOVERY:\n" +
        "      • 'NOT_FOUND': Appointment no longer exists. Tell the user and offer to list appointments.\n" +
        "      • 'VALIDATION_ERROR' / past datetime: The new datetime is in the past.\n" +
        "          Ask for a future date/time.\n" +
        "      • 'VALIDATION_ERROR' / duplicate: Updated details match another appointment.\n" +
        "          Tell the user and ask if they want different details or if it was a mistake.\n\n" +

        "REQUIRED (pass existing value for any field the user did not change):\n" +
        "  id, appointmentType, title, provider, appointmentDateTime.\n\n" +

        "CONDITIONAL REQUIRED:\n" +
        "  customType: Required ONLY when appointmentType is 'Other'.\n\n" +

        "OPTIONAL (two-attempt rule; keep existing value if user doesn\\'t answer):\n" +
        "  reminderOffsetMinutes: Minutes before appointment; trigger must be strictly in the future.\n" +
        "  notes: Free-text notes.\n\n" +

        "PARTIAL SUCCESS: Report this operation's result independently. If the appointment update\n" +
        "  succeeds but a related reminder update (via a second tool call) fails, report both\n" +
        "  outcomes separately and honestly — never summarise as a single 'all done'.\n\n" +

        "BACKEND ENFORCES (relay any error returned; do not pre-validate beyond STEP 3 checks):\n" +
        "  - appointmentDateTime must be strictly in the future.\n" +
        "  - Updated type+title+provider+datetime must not duplicate another appointment.\n\n" +

        "LANGUAGE: Always respond in the same language the user is using (Arabic or English).";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;

        if (!Guid.TryParse(p.GetString("id"), out var id))
            return new ToolResult(false, "Invalid appointment id.");

        if (!Enum.TryParse<AppointmentType>(p.GetString("appointmentType"), ignoreCase: true, out var apptType))
            return new ToolResult(false, "Invalid appointmentType. Valid values: DoctorVisit, LabTest, Imaging, Vaccination, Dentist, Therapy, FollowUp, Other.");

        if (!DateTime.TryParse(p.GetString("appointmentDateTime"), out var apptDateTime))
            return new ToolResult(false, "Invalid appointmentDateTime. Use ISO-8601 format.");

        var customType = p.TryGetProperty("customType", out var ct) ? ct.GetString() : null;
        if (apptType == AppointmentType.Other && string.IsNullOrWhiteSpace(customType))
            return new ToolResult(false, "customType is required when appointmentType is 'Other'.");

        int? reminderOffset = p.TryGetProperty("reminderOffsetMinutes", out var ro) ? ro.GetInt32() : null;

        try
        {
            var result = await mediator.Send(
                new UpdateAppointmentCommand(
                    id, apptType, customType,
                    p.GetString("title") ?? string.Empty,
                    p.GetString("provider") ?? string.Empty,
                    apptDateTime, reminderOffset,
                    p.TryGetProperty("notes", out var notes) ? notes.GetString() : null),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to update appointment.");

            return new ToolResult(true, "Appointment updated successfully.", result.Data);
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
