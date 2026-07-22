using System.Text.Json;

namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// Converts the AI model's raw (untrusted) intent-classification output into a
/// <see cref="ParsedHealthQueryIntent"/> that is guaranteed to only ever contain
/// values from the fixed allowlists. Anything the model returns that isn't a
/// recognized category/operation/timeframe is silently dropped, never surfaced
/// as an error and never passed through to a repository query.
/// </summary>
public static class HealthQueryIntentParser
{
    // Defensive aliases for near-miss category names a model might plausibly emit
    // despite the prompt's fixed vocabulary (e.g. the "LabResults" sub-concept has
    // no separate repository of its own - it's always rendered as part of LabReports).
    private static readonly Dictionary<string, HealthQueryCategory> CategoryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LabResults"]           = HealthQueryCategory.LabReports,
        ["LabResult"]            = HealthQueryCategory.LabReports,
        ["Labs"]                 = HealthQueryCategory.LabReports,
        ["Tests"]                = HealthQueryCategory.LabReports,
        ["Medications"]          = HealthQueryCategory.Medicines,
        ["Medication"]           = HealthQueryCategory.Medicines,
        ["Drugs"]                = HealthQueryCategory.Medicines,
        ["Diseases"]             = HealthQueryCategory.ChronicDiseases,
        ["Conditions"]           = HealthQueryCategory.ChronicDiseases,
        ["Condition"]            = HealthQueryCategory.ChronicDiseases,
        ["Imaging"]              = HealthQueryCategory.ImagingReports,
        ["Radiology"]            = HealthQueryCategory.ImagingReports,
        ["Vitals"]               = HealthQueryCategory.Profile,
        ["Summary"]              = HealthQueryCategory.Profile,
        ["Reminders"]            = HealthQueryCategory.MedicationReminders,
        ["MedicineReminders"]    = HealthQueryCategory.MedicationReminders,
        ["MedicationSchedule"]   = HealthQueryCategory.MedicationReminders,
        ["Schedule"]             = HealthQueryCategory.MedicationReminders,
        ["Family"]               = HealthQueryCategory.FamilyOverview,
        ["FamilyMembers"]        = HealthQueryCategory.FamilyOverview,
        ["FamilySummary"]        = HealthQueryCategory.FamilyOverview,
    };

    public static ParsedHealthQueryIntent Parse(string? rawModelOutput)
    {
        var raw = TryDeserialize(rawModelOutput);
        if (raw is null)
            return ParsedHealthQueryIntent.Empty;

        var categories = new List<HealthQueryCategory>();
        foreach (var rawCategory in raw.Categories)
        {
            if (string.IsNullOrWhiteSpace(rawCategory))
                continue;

            var trimmed = rawCategory.Trim();

            if (Enum.TryParse<HealthQueryCategory>(trimmed, ignoreCase: true, out var category)
                || CategoryAliases.TryGetValue(trimmed, out category))
            {
                if (!categories.Contains(category))
                    categories.Add(category);
            }
        }

        var operation = Enum.TryParse<HealthQueryOperation>(raw.Operation?.Trim(), ignoreCase: true, out var parsedOperation)
            ? parsedOperation
            : HealthQueryOperation.List;

        var timeframe = Enum.TryParse<HealthQueryTimeframe>(raw.Timeframe?.Trim(), ignoreCase: true, out var parsedTimeframe)
            ? parsedTimeframe
            : HealthQueryTimeframe.None;

        var searchTerm = string.IsNullOrWhiteSpace(raw.SearchTerm) ? null : raw.SearchTerm.Trim();

        // A general "summarize my health" request with no explicit category still
        // needs somewhere to route to - treat it as the whole-profile overview.
        if (categories.Count == 0 && operation == HealthQueryOperation.Summary)
            categories.Add(HealthQueryCategory.Profile);

        // Sanitize the AI-provided target hint: trim, cap length, reject empty strings.
        var targetProfileHint = string.IsNullOrWhiteSpace(raw.TargetProfile)
            ? null
            : raw.TargetProfile.Trim()[..Math.Min(raw.TargetProfile.Trim().Length, 100)];

        return new ParsedHealthQueryIntent(categories, operation, searchTerm, timeframe, targetProfileHint);
    }

    private static HealthQueryIntent? TryDeserialize(string? rawModelOutput)
    {
        if (string.IsNullOrWhiteSpace(rawModelOutput))
            return null;

        var text = rawModelOutput.Trim();

        // Defensive cleanup: models occasionally wrap JSON in a markdown code fence
        // despite explicit instructions not to.
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0)
                text = text[(firstNewline + 1)..];
            text = text.TrimEnd();
            if (text.EndsWith("```"))
                text = text[..^3];
            text = text.Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<HealthQueryIntent>(
                text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
