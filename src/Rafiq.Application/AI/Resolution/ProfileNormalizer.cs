using System.Text.RegularExpressions;

namespace Rafiq.Application.AI.Resolution;

/// <summary>
/// Normalizes Arabic and English text for consistent matching:
/// removes tashkeel, collapses alef variants, maps ة→ه and ى→ي, lowercases.
/// Dictionary keys for Arabic terms must be pre-normalized using this same method.
/// </summary>
public static class ProfileNormalizer
{
    private static readonly Regex TashkeelPattern =
        new(@"[ً-ٰٟ]", RegexOptions.Compiled);

    private static readonly Regex AlefVariantsPattern =
        new(@"[أإآٱ]", RegexOptions.Compiled);

    private static readonly Regex WhitespacePattern =
        new(@"\s+", RegexOptions.Compiled);

    private static readonly char[] TokenSeparators =
        [' ', ',', '،', '؟', '?', '.', '!', ':', ';', '(', ')', '"', '\'', '-'];

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = TashkeelPattern.Replace(input, string.Empty);
        s = AlefVariantsPattern.Replace(s, "ا");
        s = s.Replace('ة', 'ه').Replace('ى', 'ي');
        s = s.ToLowerInvariant();
        return WhitespacePattern.Replace(s.Trim(), " ");
    }

    public static IReadOnlyList<string> Tokenize(string normalizedText) =>
        normalizedText.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
}
