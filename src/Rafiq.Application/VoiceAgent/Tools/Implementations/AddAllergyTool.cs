using MediatR;
using Rafiq.Application.Features.PatientProfiles.Commands.Allergies.CreateAllergy;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class AddAllergyTool(ISender mediator) : IVoiceTool
{
    public string Name => "add_allergy";
    public string Description => "Adds a new allergy to the profile (self or family member).";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"name\",\"severity\"]," +
        "\"properties\":{" +
        "\"name\":{\"type\":\"string\",\"description\":\"Allergen name (e.g. 'Penicillin', 'Peanuts')\"}," +
        "\"severity\":{\"type\":\"string\",\"enum\":[\"Mild\",\"Moderate\",\"Severe\",\"LifeThreatening\"]}," +
        "\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: family member profileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => ["list_allergies", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user wants to add / record a new allergy.\n" +
        "Arabic triggers: ضيف حساسية، سجل حساسية، عندي حساسية من ...، اضف حساسية جديدة\n\n" +

        "REQUIRED FIELDS — collect all before calling:\n" +
        "  name:     Allergen name (drug, food, environmental). Must come from the user.\n" +
        "  severity: One of Mild, Moderate, Severe, LifeThreatening.\n" +
        "            Map naturally:\n" +
        "              'خفيف' / 'بسيط' / 'mild'           → Mild\n" +
        "              'متوسط' / 'moderate'               → Moderate\n" +
        "              'شديد' / 'severe'                  → Severe\n" +
        "              'مهدد للحياة' / 'life-threatening' → LifeThreatening\n" +
        "  If severity is not stated, ASK before calling. Never default silently.\n\n" +

        "FAMILY MEMBER CONTEXT: If for a family member, pass their profileId as targetProfileId.\n\n" +

        "Only claim success AFTER this tool returns success=true.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var name = request.Parameters.GetString("name");
        if (string.IsNullOrWhiteSpace(name))
            return new ToolResult(false, "Allergen name is required.");

        if (!Enum.TryParse<AllergySeverity>(
                request.Parameters.GetString("severity"), ignoreCase: true, out var severity))
            return new ToolResult(false, "Invalid severity. Valid values: Mild, Moderate, Severe, LifeThreatening.");

        try
        {
            var result = await mediator.Send(
                new CreateAllergyCommand(context.ProfileId, name, severity), cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to add allergy.");

            return new ToolResult(true, "Allergy added successfully.", result.Data, EntityType: "Allergy");
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
    }
}
