namespace Rafiq.Application.AI.HealthQuery;

/// <summary>
/// A small, deliberately non-exhaustive synonym table so a colloquial Egyptian-Arabic
/// or English search term (e.g. "سكر") also matches records stored under their common
/// clinical name (e.g. "Glucose"). This is a deterministic backstop, not a translation
/// engine - the intent-classification prompt already asks the model to normalize common
/// terms itself; this just widens recall for the cases it doesn't.
/// </summary>
public static class MedicalTermSynonyms
{
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["سكر"] = ["glucose", "sugar", "hba1c", "a1c"],
        ["السكر"] = ["glucose", "sugar", "hba1c", "a1c"],
        ["ضغط"] = ["blood pressure", "pressure", "bp"],
        ["الضغط"] = ["blood pressure", "pressure", "bp"],
        ["كبد"] = ["liver", "alt", "ast", "sgpt", "sgot"],
        ["الكبد"] = ["liver", "alt", "ast", "sgpt", "sgot"],
        ["كلى"] = ["kidney", "creatinine", "urea", "renal"],
        ["الكلى"] = ["kidney", "creatinine", "urea", "renal"],
        ["دهون"] = ["cholesterol", "lipid", "triglycerides", "ldl", "hdl"],
        ["الدهون"] = ["cholesterol", "lipid", "triglycerides", "ldl", "hdl"],
        ["دم"] = ["blood", "cbc", "hemoglobin"],
        ["الدم"] = ["blood", "cbc", "hemoglobin"],
        ["غدة"] = ["thyroid", "tsh"],
        ["الغدة"] = ["thyroid", "tsh"],
        ["بنسلين"] = ["penicillin"],
        ["البنسلين"] = ["penicillin"],
        ["أسبرين"] = ["aspirin"],
        ["الأسبرين"] = ["aspirin"]
    };

    /// <summary>
    /// Returns the original term plus any known synonyms, for use as an "any of these
    /// substrings match" filter. Always includes the original (trimmed) term first.
    /// </summary>
    public static IReadOnlyList<string> Expand(string term)
    {
        var trimmed = term.Trim();
        if (trimmed.Length == 0)
            return Array.Empty<string>();

        if (!Synonyms.TryGetValue(trimmed, out var extra))
            return [trimmed];

        var result = new List<string>(extra.Length + 1) { trimmed };
        result.AddRange(extra);
        return result;
    }
}
