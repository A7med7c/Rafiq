using System.Globalization;
using MediatR;
using Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.CreateChronicDisease;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class AddChronicDiseaseTool(ISender mediator) : IVoiceTool
{
    public string Name => "add_chronic_disease";
    public string Description => "Adds a new chronic disease to the profile (self or family member).";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"name\",\"status\"]," +
        "\"properties\":{" +
        "\"name\":{\"type\":\"string\",\"description\":\"Disease name (e.g. 'Diabetes', 'Hypertension')\"}," +
        "\"status\":{\"type\":\"string\",\"enum\":[\"Active\",\"Controlled\",\"Resolved\"]}," +
        "\"diagnosedAt\":{\"type\":\"string\",\"format\":\"date\",\"description\":\"Optional ISO-8601 date (yyyy-MM-dd)\"}," +
        "\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: family member profileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => ["list_chronic_diseases", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user wants to add / record a new chronic disease.\n" +
        "Arabic triggers: ضيف مرض مزمن، سجل مرض، عندي سكر، عندي ضغط، مرض جديد\n\n" +

        "REQUIRED FIELDS — collect all before calling:\n" +
        "  name:   Disease name. Must come from the user.\n" +
        "  status: One of Active, Controlled, Resolved.\n" +
        "          Map naturally:\n" +
        "            'نشط' / 'active' / user just said 'I have X'         → Active\n" +
        "            'متحكم فيه' / 'controlled' / 'مسيطر عليه'             → Controlled\n" +
        "            'شافي' / 'resolved' / 'خلاص خف'                       → Resolved\n" +
        "          Default to Active only if the user says 'I have X' without qualification.\n\n" +

        "OPTIONAL:\n" +
        "  diagnosedAt: ISO-8601 date (yyyy-MM-dd). Ask if user mentions when.\n\n" +

        "FAMILY MEMBER CONTEXT: If for a family member, pass their profileId as targetProfileId.\n\n" +

        "Only claim success AFTER this tool returns success=true.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var name = request.Parameters.GetString("name");
        if (string.IsNullOrWhiteSpace(name))
            return new ToolResult(false, "Disease name is required.");

        if (!Enum.TryParse<DiseaseStatus>(
                request.Parameters.GetString("status"), ignoreCase: true, out var status))
            return new ToolResult(false, "Invalid status. Valid values: Active, Controlled, Resolved.");

        DateOnly? diagnosedAt = null;
        var diagnosedStr = request.Parameters.GetString("diagnosedAt");
        if (!string.IsNullOrWhiteSpace(diagnosedStr))
        {
            if (!DateOnly.TryParse(diagnosedStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return new ToolResult(false, "Invalid diagnosedAt. Use ISO-8601 date format (yyyy-MM-dd).");
            diagnosedAt = parsed;
        }

        try
        {
            var result = await mediator.Send(
                new CreateChronicDiseaseCommand(context.ProfileId, name, diagnosedAt, status),
                cancellationToken);

            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to add chronic disease.");

            return new ToolResult(true, "Chronic disease added successfully.", result.Data, EntityType: "ChronicDisease");
        }
        catch (ValidationException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "VALIDATION_ERROR");
        }
    }
}
