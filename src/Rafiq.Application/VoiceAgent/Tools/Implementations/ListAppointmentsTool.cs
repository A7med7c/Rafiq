using MediatR;
using Rafiq.Application.Features.Appointments.Queries.GetAppointments;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListAppointmentsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_appointments";
    public string Description => "Returns ALL appointments (past, upcoming, cancelled, missed) for this profile.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";
    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about all appointments, past appointments, or appointment history.\n" +
        "For 'upcoming' or 'future' appointments, prefer list_upcoming_appointments instead.\n\n" +
        "Result fields: id, appointmentType, customType, title, provider, " +
        "appointmentDateTime (UTC ISO-8601), status (Upcoming/Completed/Cancelled/Missed), " +
        "reminderOffsetMinutes, notes.\n\n" +
        "DISPLAY RULE — appointmentDateTime values are in UTC. " +
        "ALWAYS convert to the user's local time before showing them. " +
        "Apply: UTC time + UtcOffsetMinutes = local time. " +
        "Never show UTC times or say 'UTC' / 'GMT' to the user. " +
        "Format naturally, e.g. 'Monday 21 July at 5:00 PM' or 'الاثنين 21 يوليو الساعة 5:00 مساءً'.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAppointmentsQuery(context.ProfileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve appointments.");

        var appointments = result.Data ?? [];
        return new ToolResult(true,
            appointments.Count == 0 ? "No appointments found." : $"Found {appointments.Count} appointment(s).",
            new { appointments });
    }
}
