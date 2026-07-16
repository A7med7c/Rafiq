namespace Rafiq.Application.AI.Resolution;

/// <summary>
/// Detects when the user is clearly asking about their own health data ("my medications",
/// "عندي كام دواء؟"). Runs after relationship detection fails, so "my father" never
/// triggers this — "father" is caught first by RelationshipTermDetector.
/// </summary>
public static class SelfReferenceDetector
{
    private static readonly HashSet<string> SelfTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // English
        "my", "mine", "i",
        // Arabic (pre-normalized)
        "عندي", "بتاعتي", "تاعتي", "ليا", "معايا", "انا", "بتاعي", "معي",
    };

    // Normalized English phrase prefixes that indicate self-reference
    private static readonly string[] SelfPhraseStarts =
    [
        "do i ", "have i ", "am i ", "what are my ", "show me my ",
        "what's my ", "what is my ", "how many do i ", "how many of my ", "list my ",
    ];

    public static bool IsConfident(string message)
    {
        var normalized = ProfileNormalizer.Normalize(message);
        var tokens = ProfileNormalizer.Tokenize(normalized);

        foreach (var token in tokens)
        {
            if (SelfTokens.Contains(token))
                return true;
        }

        foreach (var phrase in SelfPhraseStarts)
        {
            if (normalized.StartsWith(phrase, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
