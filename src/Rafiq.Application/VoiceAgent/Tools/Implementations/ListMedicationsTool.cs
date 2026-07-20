using MediatR;
using Rafiq.Application.Features.UserMedicines.Queries.GetMyUserMedicines;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListMedicationsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_medications";
    public string Description => "Returns all medications for this profile.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{},\"required\":[]}";
    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their medications, drug list, or medicines.\n" +
        "Also call this BEFORE update_medication or delete_medication to find the correct medication id.\n" +
        "Result fields per medication: id (use this for update/delete), medicineName, dosage, " +
        "frequency, duration, notes, source (Manual/Prescription/MedicineBox), createdAt.\n" +
        "If the user says 'update Paracetamol' — list first, find the id, then call update_medication.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyUserMedicinesQuery(context.ProfileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve medications.");

        var medications = result.Data ?? [];
        return new ToolResult(true,
            medications.Count == 0 ? "No medications found." : $"Found {medications.Count} medication(s).",
            new { medications });
    }
}
