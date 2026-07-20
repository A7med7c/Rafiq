using MediatR;
using Rafiq.Application.Features.Appointments.Commands.CancelAppointment;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class CancelAppointmentTool(ISender mediator) : IVoiceTool
{
    public string Name => "cancel_appointment";
    public string Description => "Cancels an upcoming appointment.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — FIND the appointment.\n" +
        "    Call list_upcoming_appointments.\n" +
        "    Filter by type, title, provider, date, or keywords the user mentioned.\n" +
        "    ▸ No match (including past/completed dates) → tell the user:\n" +
        "      'I couldn't find a matching upcoming appointment to cancel.\n" +
        "       Past or completed appointments cannot be cancelled.'\n" +
        "      Do not call cancel_appointment.\n" +
        "    ▸ Multiple matches → list them and ask which one:\n" +
        "      '1. Dentist appointment on Monday 21 July at 4:00 PM\n" +
        "       2. Dentist appointment on Friday 25 July at 10:00 AM\n" +
        "       Which one would you like to cancel?'\n" +
        "    ▸ Exactly one match → go to STEP 2.\n\n" +

        "  STEP 2 — CONFIRM (always, before cancelling).\n" +
        "    Ask: 'Are you sure you want to cancel your [title] appointment with [provider]\n" +
        "    on [date] at [time]?'\n" +
        "    Only call cancel_appointment after the user explicitly says yes.\n\n" +

        "  STEP 3 — CALL cancel_appointment with the appointment id.\n\n" +

        "REQUIRED:\n" +
        "  id: UUID of the appointment to cancel (obtained from list_upcoming_appointments).\n\n" +

        "BACKEND ENFORCES:\n" +
        "  - Only Upcoming appointments can be cancelled.\n" +
        "  - Completed, Cancelled, or Missed appointments cannot be cancelled.\n" +
        "  - Any scheduled reminder will also be cancelled.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("id"), out var id))
            return new ToolResult(false, "Invalid appointment id.");

        try
        {
            var result = await mediator.Send(new CancelAppointmentCommand(id), cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to cancel appointment.");

            return new ToolResult(true, "Appointment has been cancelled.");
        }
        catch (BadRequestException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "INVALID_STATUS");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
    }
}
