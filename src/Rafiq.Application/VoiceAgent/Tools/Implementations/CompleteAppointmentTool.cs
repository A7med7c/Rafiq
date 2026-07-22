using MediatR;
using Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class CompleteAppointmentTool(ISender mediator) : IVoiceTool
{
    public string Name => "complete_appointment";
    public string Description => "Marks an upcoming appointment as completed.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"id\"]," +
        "\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"format\":\"uuid\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when the user says they've attended an appointment and want to mark it as done.\n\n" +
        "BEFORE CALLING — you need the appointment id:\n" +
        "  Call list_upcoming_appointments, identify the appointment, use its id.\n\n" +
        "REQUIRED:\n" +
        "  id: UUID of the appointment to mark as completed.\n\n" +
        "BUSINESS RULES:\n" +
        "  - Only Upcoming appointments can be marked as completed.\n" +
        "  - Confirm with the user which appointment to complete if there are multiple upcoming ones.\n\n" +
        "CONVERSATION HINT:\n" +
        "  'Which appointment did you attend?' → list upcoming ones → user picks one → confirm → execute.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("id"), out var id))
            return new ToolResult(false, "Invalid appointment id.");

        try
        {
            var result = await mediator.Send(new CompleteAppointmentCommand(id), cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to complete appointment.");

            return new ToolResult(true, "Appointment marked as completed.", result.Data);
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
