using MediatR;
using Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.VoiceAgent.Tools.Implementations;

public sealed class DeleteFamilyMemberTool(ISender mediator) : IVoiceTool
{
    public string Name => "delete_family_member";
    public string Description => "Permanently deletes a managed family member profile and all their health data (medications, appointments, medical records, reminders). Only the profile Owner can delete.";

    public string ParameterSchema =>
        "{\"type\":\"object\"," +
        "\"required\":[\"profileId\"]," +
        "\"properties\":{" +
        "\"profileId\":{\"type\":\"string\",\"format\":\"uuid\",\"description\":\"userHealthProfileId from list_family_profiles\"}" +
        "}}";

    public string[] RelatedToolNames => [];

    public string DomainContext =>
        "WORKFLOW — follow these steps in order:\n\n" +

        "  STEP 1 — IDENTIFY the family member.\n" +
        "    Call list_family_profiles to find their userHealthProfileId.\n" +
        "    Apply disambiguation if multiple members match.\n\n" +

        "  STEP 2 — CONFIRM before deleting (always — this cannot be undone).\n" +
        "    Show what will be deleted and explicitly ask for confirmation:\n" +
        "    EN: 'Are you sure you want to permanently delete Mohamed's profile? " +
        "This will delete ALL their health data: medications, appointments, " +
        "medical records, and reminders. This cannot be undone.'\n" +
        "    AR: 'هل أنت متأكد من حذف ملف محمد بشكل نهائي؟ " +
        "سيتم حذف جميع بياناته الصحية: الأدوية والمواعيد والسجلات الطبية والتذكيرات. " +
        "لا يمكن التراجع عن هذا الإجراء.'\n" +
        "    Only proceed after an explicit yes / نعم / confirm.\n\n" +

        "  STEP 3 — CALL delete_family_member.\n\n" +

        "REQUIRED:\n" +
        "  profileId — userHealthProfileId from list_family_profiles\n\n" +

        "PERMISSIONS:\n" +
        "  Only the profile Owner can delete. Managers and Viewers cannot.\n" +
        "  If the backend returns a permission error, relay it as-is.\n\n" +

        "SIDE EFFECTS — ALL of the following are permanently deleted:\n" +
        "  • The health profile itself\n" +
        "  • All medications and their reminders\n" +
        "  • All appointments\n" +
        "  • All medical records (lab reports, prescriptions, imaging)\n" +
        "  This action cannot be undone.";

    public async Task<ToolResult> ExecuteAsync(
        ToolCallRequest request, AgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Parameters.GetString("profileId"), out var profileId))
            return new ToolResult(false, "Invalid profileId. Provide a UUID from list_family_profiles.");

        try
        {
            var result = await mediator.Send(new DeletePatientProfileCommand(profileId), cancellationToken);
            if (!result.Success)
                return new ToolResult(false, result.Message ?? "Failed to delete profile.");

            return new ToolResult(true, "Family member profile and all associated health data have been permanently deleted.");
        }
        catch (NotFoundException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "NOT_FOUND");
        }
        catch (UnauthorizedException ex)
        {
            return new ToolResult(false, ex.Message, ErrorCode: "UNAUTHORIZED");
        }
    }
}
