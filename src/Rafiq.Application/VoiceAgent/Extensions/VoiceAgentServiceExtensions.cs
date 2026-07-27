using Microsoft.Extensions.DependencyInjection;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Application.VoiceAgent.Domain;
using Rafiq.Application.VoiceAgent.Domain.Analyzers;
using Rafiq.Application.VoiceAgent.Tools;
using Rafiq.Application.VoiceAgent.Tools.Implementations;

namespace Rafiq.Application.VoiceAgent.Extensions;

public static class VoiceAgentServiceExtensions
{
    public static IServiceCollection AddVoiceAgent(this IServiceCollection services)
    {
        // Core agent services
        services.AddScoped<ToolRegistry>();
        services.AddScoped<VoiceAgentLoop>();

        // ── Entity analyzers (proactive gap detection) ────────────────────────
        services.AddScoped<IDomainEntityAnalyzer, MedicationEntityAnalyzer>();
        services.AddScoped<IDomainEntityAnalyzer, AppointmentEntityAnalyzer>();

        // ── Read tools ────────────────────────────────────────────────────────
        services.AddScoped<IVoiceTool, GetHealthSummaryTool>();
        services.AddScoped<IVoiceTool, GetProfileInfoTool>();
        services.AddScoped<IVoiceTool, ListMedicationsTool>();
        services.AddScoped<IVoiceTool, ListAppointmentsTool>();
        services.AddScoped<IVoiceTool, ListUpcomingAppointmentsTool>();
        services.AddScoped<IVoiceTool, ListAllergiesTool>();
        services.AddScoped<IVoiceTool, ListChronicDiseasesTool>();
        services.AddScoped<IVoiceTool, ListPrescriptionsTool>();
        services.AddScoped<IVoiceTool, ListLabReportsTool>();
        services.AddScoped<IVoiceTool, ListImagingReportsTool>();
        services.AddScoped<IVoiceTool, ListDocumentsTool>();
        services.AddScoped<IVoiceTool, ListEmergencyContactsTool>();

        // ── Medication write tools ────────────────────────────────────────────
        services.AddScoped<IVoiceTool, AddMedicationTool>();
        services.AddScoped<IVoiceTool, UpdateMedicationTool>();
        services.AddScoped<IVoiceTool, DeleteMedicationTool>();

        // ── Medication reminder tools ─────────────────────────────────────────
        services.AddScoped<IVoiceTool, AddMedicationReminderTool>();
        services.AddScoped<IVoiceTool, ListMedicationRemindersTool>();
        services.AddScoped<IVoiceTool, UpdateMedicationReminderTool>();
        services.AddScoped<IVoiceTool, DeleteMedicationReminderTool>();
        services.AddScoped<IVoiceTool, ToggleMedicationReminderTool>();

        // ── Appointment write tools ───────────────────────────────────────────
        services.AddScoped<IVoiceTool, BookAppointmentTool>();
        services.AddScoped<IVoiceTool, UpdateAppointmentTool>();
        services.AddScoped<IVoiceTool, CancelAppointmentTool>();
        services.AddScoped<IVoiceTool, CompleteAppointmentTool>();

        // ── Allergy / chronic disease write tools ─────────────────────────────
        services.AddScoped<IVoiceTool, AddAllergyTool>();
        services.AddScoped<IVoiceTool, AddChronicDiseaseTool>();

        // ── Family profile tools ──────────────────────────────────────────────
        services.AddScoped<IVoiceTool, ListFamilyProfilesTool>();
        services.AddScoped<IVoiceTool, GetFamilyMemberDetailsTool>();
        services.AddScoped<IVoiceTool, AddFamilyMemberTool>();
        services.AddScoped<IVoiceTool, UpdateFamilyMemberTool>();
        services.AddScoped<IVoiceTool, DeleteFamilyMemberTool>();

        return services;
    }
}
