using MediatR;
using Rafiq.Application.Features.Appointments.Commands.CreateAppointment;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class BookAppointmentTool(ISender mediator) : IVoiceTool
{
    public string Name => "book_appointment";
    public string Description => "Books a new medical appointment.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"appointmentType\",\"title\",\"provider\",\"appointmentDateTime\"]," +
        "\"properties\":{" +
        "\"appointmentType\":{\"type\":\"string\"}," +
        "\"customType\":{\"type\":\"string\"}," +
        "\"title\":{\"type\":\"string\"}," +
        "\"provider\":{\"type\":\"string\"}," +
        "\"appointmentDateTime\":{\"type\":\"string\",\"format\":\"date-time\"}," +
        "\"reminderOffsetMinutes\":{\"type\":\"integer\",\"minimum\":0,\"description\":\"OPTIONAL — leave for update_appointment after saving; the reminder is asked about AFTER the appointment is created\"}," +
        "\"notes\":{\"type\":\"string\"}," +
        "\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: family member profileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => ["update_appointment", "list_family_profiles"];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — COLLECT the REQUIRED fields (see REQUIRED below).\n" +
        "    Ask one at a time, naturally. Keep asking until EACH required field is confirmed.\n" +
        "    Never call book_appointment until all required fields are present.\n\n" +

        "  STEP 2 — OPTIONAL notes (ask at most ONCE, twice max if genuinely ambiguous).\n" +
        "    If user declines / skips / ignores → omit notes and continue.\n" +
        "    Do NOT ask about the reminder yet — that comes AFTER saving.\n\n" +

        "  STEP 3 — CALL book_appointment (WITHOUT reminderOffsetMinutes) and WAIT for the\n" +
        "    tool result. Only claim success AFTER the tool returns success=true. If the tool\n" +
        "    returns success=false, relay the error — never fabricate 'تم الحجز' or 'booked'.\n\n" +

        "  STEP 4 — AFTER a successful save, ask about the reminder.\n" +
        "    EN: 'How long before the appointment would you like a reminder?'\n" +
        "    AR: 'قبل الموعد بكام تحب تجيلك تذكير؟'\n" +
        "    If user gives a time (e.g. '30 minutes', 'ساعة'):\n" +
        "      • Apply REMINDER VALIDATION below.\n" +
        "      • If valid → call update_appointment with the appointment id and the\n" +
        "        new reminderOffsetMinutes to attach the reminder.\n" +
        "      • If invalid → tell the user and re-ask for a shorter offset.\n" +
        "    If user declines / says 'no' / 'skip' / 'لا' / 'مش عايز' → finish naturally.\n\n" +

        "REQUIRED — collect all four before calling book_appointment:\n" +
        "  appointmentType:  DoctorVisit, LabTest, Imaging, Vaccination, Dentist,\n" +
        "                    Therapy, FollowUp, Other.\n" +
        "                    Map naturally: 'blood test'→LabTest, 'x-ray'→Imaging, 'dentist'→Dentist.\n" +
        "                    Arabic: 'أسنان'→Dentist, 'تحليل'→LabTest, 'أشعة'→Imaging,\n" +
        "                            'تطعيم'→Vaccination, 'متابعة'→FollowUp.\n" +
        "  title:            Brief description. Infer when obvious: 'dentist'→'Dentist Appointment'.\n" +
        "  provider:         Doctor or clinic name. Must come from the user — never invent.\n" +
        "  appointmentDateTime: DATE + TIME the user specified, combined into ISO-8601 UTC.\n" +
        "                    DATE and TIME are two separate pieces the user must provide.\n" +
        "                    Date given, time missing → ask: 'What time on [date]?'\n" +
        "                    Time given, date missing → ask: 'What date?'\n" +
        "                    NEVER default the time silently. Use 09:00 only if user says 'any time'.\n" +
        "                    Apply DATE/TIME RESOLUTION rules from the main prompt.\n\n" +

        "CONDITIONAL REQUIRED:\n" +
        "  customType: Required ONLY when appointmentType is 'Other'.\n\n" +

        "OPTIONAL — ask at most once each; skip if user declines:\n" +
        "  notes: Reason for visit.\n\n" +

        "DATE/TIME VALIDATION — apply before calling the tool:\n" +
        "  appointmentDateTime MUST be strictly in the future (later than NOW UTC).\n" +
        "  If the user's date/time is in the past → ask for a future date/time,\n" +
        "  do not call the tool.\n\n" +

        "REMINDER VALIDATION — apply in STEP 4 before update_appointment:\n" +
        "  The reminder trigger time = appointmentDateTime − reminderOffsetMinutes.\n" +
        "  It MUST be strictly in the future (later than NOW UTC).\n" +
        "  If reminderOffsetMinutes >= minutes-until-appointment → REJECT with:\n" +
        "    EN: 'A [X]-minute reminder would be at or before now. Please choose a shorter time.'\n" +
        "    AR: 'التذكير قبل [X] دقيقة سيكون الآن أو في الماضي. اختر مدة أقصر من فضلك.'\n" +
        "  Only proceed once the offset is valid or the user explicitly waives the reminder.\n\n" +

        "FAMILY MEMBER CONTEXT: If the appointment is for a family member,\n" +
        "  pass their profileId as targetProfileId in the tool call.\n\n" +

        "BACKEND ENFORCES (do not pre-validate yourself — relay any error returned):\n" +
        "  - appointmentDateTime must be in the future (UTC).\n" +
        "  - Cannot book a duplicate: same type+title+provider+datetime already exists.\n\n" +

        "The result data includes the created appointment id — use it in STEP 4 for the reminder.\n\n" +

        "LANGUAGE: Always respond in the same language the user is using (Arabic or English).";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var p = request.Parameters;

        if (!Enum.TryParse<AppointmentType>(p.GetString("appointmentType"), ignoreCase: true, out var apptType))
            return new ToolResult(false, "Invalid appointmentType. Valid values: DoctorVisit, LabTest, Imaging, Vaccination, Dentist, Therapy, FollowUp, Other.");

        // Parse with RoundtripKind so the 'Z' suffix is honoured and the resulting
        // DateTime always has Kind=Utc, avoiding local-timezone contamination.
        if (!DateTimeOffset.TryParse(
                p.GetString("appointmentDateTime"),
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var apptDateTimeOffset))
            return new ToolResult(false, "Invalid appointmentDateTime. Use ISO-8601 UTC format (e.g. 2026-07-25T14:00:00Z).");

        var apptDateTime = apptDateTimeOffset.UtcDateTime; // Kind = Utc

        var customType = p.TryGetProperty("customType", out var ct) ? ct.GetString() : null;
        if (apptType == AppointmentType.Other && string.IsNullOrWhiteSpace(customType))
            return new ToolResult(false, "customType is required when appointmentType is 'Other'. Please provide a description.");

        int? reminderOffset = p.TryGetProperty("reminderOffsetMinutes", out var ro)
            ? ro.GetInt32()
            : null;

        try
        {
            var result = await mediator.Send(
                new CreateAppointmentCommand(
                    context.ProfileId, apptType, customType,
                    p.GetString("title") ?? string.Empty,
                    p.GetString("provider") ?? string.Empty,
                    apptDateTime, reminderOffset,
                    p.TryGetProperty("notes", out var notes) ? notes.GetString() : null),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to book appointment.");

            return new ToolResult(true, "Appointment booked successfully.", result.Data, EntityType: "Appointment");
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
    }
}
