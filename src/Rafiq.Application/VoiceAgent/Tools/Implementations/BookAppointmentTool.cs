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
        "\"reminderOffsetMinutes\":{\"type\":\"integer\",\"minimum\":0}," +
        "\"notes\":{\"type\":\"string\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — COLLECT all required fields (see REQUIRED below).\n" +
        "    Never call any tool until all five required fields are confirmed.\n\n" +

        "  STEP 2 — CHECK FOR CONFLICTS.\n" +
        "    Call list_upcoming_appointments and scan the results.\n" +
        "    If any appointment shares the same date AND time the user requested:\n" +
        "      Tell the user: 'You already have a [type] appointment with [provider]\n" +
        "      on [date] at [time]. Would you like to choose a different time?'\n" +
        "    Do not proceed to step 3 until the user confirms a conflict-free time.\n" +
        "    If no conflict is found, continue immediately.\n\n" +

        "  STEP 3 — ASK ABOUT OPTIONAL FIELDS (at most once each):\n" +
        "    Reminder: 'Would you like a reminder before the appointment?'\n" +
        "      If user declines / says no / ignores → omit reminderOffsetMinutes.\n" +
        "      If user agrees → ask how many minutes before. Apply REMINDER VALIDATION below.\n" +
        "    Notes: ask only if naturally relevant in context. Default: omit.\n\n" +

        "  STEP 4 — CALL book_appointment.\n\n" +

        "REQUIRED — collect all five before any tool call:\n" +
        "  appointmentType:  DoctorVisit, LabTest, Imaging, Vaccination, Dentist,\n" +
        "                    Therapy, FollowUp, Other.\n" +
        "                    Map naturally: 'blood test'→LabTest, 'x-ray'→Imaging, 'dentist'→Dentist.\n" +
        "  title:            Brief description. Infer when obvious: 'dentist'→'Dentist Appointment'.\n" +
        "  provider:         Doctor or clinic name. Must come from the user — never invent.\n" +
        "  appointmentDate:  The DATE the user specified. Apply DATE/TIME RESOLUTION rules.\n" +
        "  appointmentTime:  The TIME the user specified. Apply DATE/TIME RESOLUTION rules.\n" +
        "                    DATE and TIME are two separate fields.\n" +
        "                    Date given, time missing → ask: 'What time on [date]?'\n" +
        "                    Time given, date missing → ask: 'What date?'\n" +
        "                    NEVER default the time. Only use 09:00 if user says 'any time'.\n" +
        "                    Combine into appointmentDateTime (ISO-8601 UTC) for the tool call.\n\n" +

        "CONDITIONAL REQUIRED:\n" +
        "  customType: Required ONLY when appointmentType is 'Other'.\n\n" +

        "OPTIONAL — ask at most once each; skip if user declines:\n" +
        "  reminderOffsetMinutes: Minutes before to notify.\n" +
        "  notes:                 Reason for visit.\n\n" +

        "REMINDER VALIDATION — apply before calling the tool:\n" +
        "  Compute: appointmentDateTime minus reminderOffsetMinutes.\n" +
        "  If that computed time is already in the past (before NOW UTC):\n" +
        "    Tell the user: 'A [X]-minute reminder would already be in the past.\n" +
        "    Would you like a shorter reminder, or skip it?'\n" +
        "  Only proceed once the reminder is valid or explicitly waived.\n\n" +

        "BACKEND ENFORCES (do not pre-validate yourself — relay any error returned):\n" +
        "  - appointmentDateTime must be in the future (UTC).\n" +
        "  - Cannot book a duplicate: same type+title+provider+datetime already exists.\n\n" +

        "The result data includes the created appointment id and its full state.";

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
