using MediatR;
using Rafiq.Application.Features.EmergencyContacts.Queries.GetEmergencyContacts;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListEmergencyContactsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_emergency_contacts";
    public string Description => "Returns all emergency contacts for this user.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";
    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their emergency contacts, " +
        "who to call in an emergency, or ICE contacts.\n" +
        "Arabic triggers: جهات الطوارئ، مين يتصل بيه، رقم الطوارئ، جهات الاتصال في الطوارئ\n\n" +
        "Each contact includes: id, name, phoneNumber, relation.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEmergencyContactsQuery(), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve emergency contacts.");

        var contacts = result.Data ?? [];
        return new ToolResult(true,
            contacts.Count == 0 ? "No emergency contacts found." : $"Found {contacts.Count} emergency contact(s).",
            new { emergencyContacts = contacts });
    }
}
