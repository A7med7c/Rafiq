using MediatR;
using Rafiq.Application.Features.Appointments.Queries.GetUpcomingAppointments;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListUpcomingAppointmentsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_upcoming_appointments";
    public string Description => "Returns only future (Upcoming status) appointments for this profile.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";
    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL in these situations:\n" +
        "  • User asks about upcoming, scheduled, or future appointments.\n" +
        "  • BEFORE book_appointment — check for conflicts at the requested date+time.\n" +
        "  • BEFORE cancel_appointment or update_appointment — find the appointment id.\n\n" +
        "Result fields per appointment: id, appointmentType, customType, title, provider,\n" +
        "  appointmentDateTime (UTC ISO-8601), reminderOffsetMinutes, notes.\n\n" +
        "When speaking to the user, format dates naturally (e.g. 'Monday July 21 at 3:00 PM').";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUpcomingAppointmentsQuery(context.ProfileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve upcoming appointments.");

        var appointments = result.Data ?? [];
        return new ToolResult(true,
            appointments.Count == 0 ? "No upcoming appointments." : $"Found {appointments.Count} upcoming appointment(s).",
            new { appointments });
    }
}
