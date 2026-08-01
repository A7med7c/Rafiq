using MediatR;
using Rafiq.Application.Features.GeneralDocuments.Queries.GetMyGeneralDocuments;
using Rafiq.Application.VoiceAgent.Agent;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class ListDocumentsTool(ISender mediator) : IVoiceTool
{
    public string Name => "list_documents";
    public string Description => "Returns all uploaded general medical documents for this profile.";
    public string ParameterSchema => "{\"type\":\"object\",\"properties\":{\"targetProfileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"Optional: userHealthProfileId from list_family_profiles; omit to use current profile\"}},\"required\":[]}";
    public string[] RelatedToolNames => ["list_lab_reports", "list_imaging_reports", "list_prescriptions", "list_family_profiles"];

    public string DomainContext =>
        "USE THIS TOOL when the user asks about their uploaded documents, medical files, " +
        "or general health documents that are not specifically prescriptions, lab reports, or imaging reports. Also works for family members.\n" +
        "Arabic triggers: المستندات، الملفات الطبية، الأوراق، مستنداتي، ملفاتي\n" +
        "Arabic family triggers: ملفات ابني، الأوراق الطبية بتاعة أمي، المستندات بتاعة أبي\n\n" +
        "Results are ordered by date descending (newest first).\n" +
        "Each document includes: id, title, documentType, uploadDate, notes.\n\n" +
        "FAMILY MEMBER CONTEXT: When asking about a family member:\n" +
        "  1. Call list_family_profiles first to find their userHealthProfileId\n" +
        "  2. Pass their id as targetProfileId in this tool call";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        var profileId = context.ProfileId;
        if (request.Parameters.TryGetProperty("targetProfileId", out var tpidProp)
            && Guid.TryParse(tpidProp.GetString(), out var familyProfileId))
        {
            profileId = familyProfileId;
        }

        var result = await mediator.Send(new GetMyGeneralDocumentsQuery(profileId), cancellationToken);
        if (!result.Success)
            return new ToolResult(false, result.Message ?? "Failed to retrieve documents.");

        var documents = result.Data ?? [];
        return new ToolResult(true,
            documents.Count == 0 ? "No documents found." : $"Found {documents.Count} document(s).",
            new { documents });
    }
}
