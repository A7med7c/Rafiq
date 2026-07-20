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
        services.AddScoped<IVoiceTool, ListMedicationsTool>();
        services.AddScoped<IVoiceTool, ListAppointmentsTool>();
        services.AddScoped<IVoiceTool, ListUpcomingAppointmentsTool>();

        // ── Medication write tools ────────────────────────────────────────────
        services.AddScoped<IVoiceTool, AddMedicationTool>();
        services.AddScoped<IVoiceTool, UpdateMedicationTool>();
        services.AddScoped<IVoiceTool, DeleteMedicationTool>();

        // ── Medication reminder tools ─────────────────────────────────────────
        services.AddScoped<IVoiceTool, AddMedicationReminderTool>();

        // ── Appointment write tools ───────────────────────────────────────────
        services.AddScoped<IVoiceTool, BookAppointmentTool>();
        services.AddScoped<IVoiceTool, UpdateAppointmentTool>();
        services.AddScoped<IVoiceTool, CancelAppointmentTool>();
        services.AddScoped<IVoiceTool, CompleteAppointmentTool>();

        return services;
    }
}
