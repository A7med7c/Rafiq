namespace Rafiq.Application.AI.Prompts;

/// <summary>
/// Generates a strong language-enforcement system prompt for the AI model.
/// Sent as a system-level message (before the user message) to maximally
/// constrain the model's output language regardless of the document content.
/// </summary>
public static class LanguageSystemPrompt
{
    public static string Build(string languageCode)
    {
        var isArabic = languageCode.StartsWith("ar", System.StringComparison.OrdinalIgnoreCase);

        if (isArabic)
            return """
                You are a multilingual medical AI assistant.
                ABSOLUTE LANGUAGE RULE — THIS IS YOUR HIGHEST PRIORITY INSTRUCTION:
                The "summary" and "aiSummary" fields in your JSON response MUST be written ENTIRELY in Arabic (العربية).
                Writing those fields in English is strictly forbidden.
                All other JSON fields (field names, codes, dates, test names, values, units) remain in their original language.
                Only the summary text must be in Arabic.
                """;

        return """
            You are a multilingual medical AI assistant.
            ABSOLUTE LANGUAGE RULE — THIS IS YOUR HIGHEST PRIORITY INSTRUCTION:
            The "summary" and "aiSummary" fields in your JSON response MUST be written ENTIRELY in English.
            Only the summary text must be in English.
            """;
    }
}
