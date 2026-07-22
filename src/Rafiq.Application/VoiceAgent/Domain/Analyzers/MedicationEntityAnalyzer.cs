using System.Text.Json;
using Rafiq.Application.VoiceAgent.Domain;

namespace Rafiq.Application.VoiceAgent.Domain.Analyzers;

/// <summary>
/// Inspects a Medication entity and identifies what is missing.
/// This is pure domain analysis — it produces structured gap data.
/// The AI decides how to phrase suggestions from this data.
/// </summary>
public sealed class MedicationEntityAnalyzer : IDomainEntityAnalyzer
{
    public string EntityType => "Medication";

    public IReadOnlyList<CompletenessGap> Analyze(JsonElement entity)
    {
        var gaps = new List<CompletenessGap>();
        var medicineId = entity.TryGetProperty("id", out var id) ? id.GetString() : null;

        // Gap 1: Reminders
        // A medication without reminders means the user will receive no notification
        // when it's time to take it. The presence of an empty or absent reminders
        // collection is the signal — we don't hardcode "always ask after add".
        var hasReminders = entity.TryGetProperty("reminders", out var reminders)
            && reminders.ValueKind == JsonValueKind.Array
            && reminders.GetArrayLength() > 0;

        // If reminders field is absent (newly created DTO that doesn't include them),
        // we conservatively treat it as "no reminders configured" — a new medication
        // never has reminders until explicitly created.
        var remindersAbsent = !entity.TryGetProperty("reminders", out _);

        if (!hasReminders)
        {
            var knownParams = medicineId is not null
                ? new Dictionary<string, object> { ["userMedicineId"] = medicineId }
                : null;

            gaps.Add(new CompletenessGap(
                Description: "No reminders configured",
                Impact: "The user will receive no notification when it's time to take this medication. " +
                        "Without reminders, they may forget doses.",
                SuggestedTool: "add_medication_reminder",
                KnownParameters: knownParams));
        }

        return gaps;
    }
}
