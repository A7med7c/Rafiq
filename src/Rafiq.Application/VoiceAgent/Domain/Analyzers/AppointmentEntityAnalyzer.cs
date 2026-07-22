using System.Text.Json;
using Rafiq.Application.VoiceAgent.Domain;

namespace Rafiq.Application.VoiceAgent.Domain.Analyzers;

/// <summary>
/// Inspects an Appointment entity and identifies what is missing.
/// </summary>
public sealed class AppointmentEntityAnalyzer : IDomainEntityAnalyzer
{
    public string EntityType => "Appointment";

    public IReadOnlyList<CompletenessGap> Analyze(JsonElement entity)
    {
        var gaps = new List<CompletenessGap>();
        var appointmentId = entity.TryGetProperty("id", out var id) ? id.GetString() : null;

        // Gap 1: No reminder configured
        // An appointment without a reminder offset means the user gets no advance notice.
        // We check for null/absent reminderOffsetMinutes.
        var hasReminder = entity.TryGetProperty("reminderOffsetMinutes", out var r)
            && r.ValueKind != JsonValueKind.Null
            && r.ValueKind == JsonValueKind.Number;

        if (!hasReminder)
        {
            var knownParams = appointmentId is not null
                ? new Dictionary<string, object> { ["id"] = appointmentId }
                : null;

            gaps.Add(new CompletenessGap(
                Description: "No reminder configured",
                Impact: "The user will not receive any notification before this appointment. " +
                        "They may miss or forget it.",
                SuggestedTool: "update_appointment",
                KnownParameters: knownParams));
        }

        // Gap 2: No notes
        // Mild gap — missing notes means no reason for the visit is documented.
        // Only surface if notes field is absent or empty, and the appointment is upcoming.
        var hasNotes = entity.TryGetProperty("notes", out var notes)
            && notes.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(notes.GetString());

        if (!hasNotes)
        {
            gaps.Add(new CompletenessGap(
                Description: "No visit notes or reason recorded",
                Impact: "Having notes helps the doctor understand the reason for the visit and " +
                        "helps the user remember what to discuss.",
                SuggestedTool: "update_appointment",
                KnownParameters: appointmentId is not null
                    ? new Dictionary<string, object> { ["id"] = appointmentId }
                    : null));
        }

        return gaps;
    }
}
