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
        "Also call this BEFORE update_medication, delete_medication, or any reminder tool\n" +
        "to find the correct medication id.\n\n" +
        "Result fields per medication: id, medicineName, dosage, frequency, duration, notes,\n" +
        "source (Manual/Prescription/MedicineBox), createdAt.\n\n" +
        "Common patterns:\n" +
        "  'Update Paracetamol' → list first, find id → call update_medication\n" +
        "  'Delete my Aspirin'  → list first, find id → confirm → call delete_medication\n" +
        "  'Show reminders for Aspirin' → list_medications to get id → call list_medication_reminders\n" +
        "  'Turn off Aspirin reminder' → list_medications → list_medication_reminders → toggle_medication_reminder";

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
