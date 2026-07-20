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
        "    ▸ No match → tell user: 'I couldn't find a matching upcoming appointment.'\n" +
        "    ▸ Exactly one match → confirm once: 'I found your [title] with [provider]\n" +
        "      on [date] at [time]. Is that the one you want to update?' Then proceed.\n" +
        "    ▸ Multiple matches → list them and ask which one:\n" +
        "      '1. Dentist appointment on Monday 21 July at 4:00 PM\n" +
        "       2. Dentist appointment on Friday 25 July at 10:00 AM\n" +
        "       Which one would you like to update?'\n\n" +

        "  STEP 2 — ASK what the user wants to change.\n" +
        "    Example: 'What would you like to update — the date, time, provider, or something else?'\n\n" +

        "  STEP 3 — COLLECT new values; keep current values for unchanged fields.\n" +
        "    For date/time: apply DATE/TIME RESOLUTION rules (same as book_appointment).\n" +
        "    If user changes date but NOT time → ask: 'What time on [new date]?' Never invent it.\n\n" +

        "  STEP 4 — CALL update_appointment.\n\n" +

        "REQUIRED (pass existing value for any field the user did not change):\n" +
        "  id, appointmentType, title, provider, appointmentDateTime.\n\n" +

        "CONDITIONAL REQUIRED:\n" +
        "  customType: Required ONLY when appointmentType is 'Other'.\n\n" +

        "OPTIONAL (keep existing value if user didn't mention it):\n" +
        "  reminderOffsetMinutes, notes.\n\n" +

        "BACKEND ENFORCES (do not pre-validate yourself — relay any error returned):\n" +
        "  - appointmentDateTime must be in the future.\n" +
        "  - Updated type+title+provider+datetime must not duplicate another appointment.";

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
