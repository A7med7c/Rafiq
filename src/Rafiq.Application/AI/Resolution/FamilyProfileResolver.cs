using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.AI.Resolution;

public sealed class FamilyProfileResolver : IFamilyProfileResolver
{
    public ProfileResolution MatchByRelationship(
        RelationshipType relationship,
        IReadOnlyList<AccessibleProfileEntry> profiles)
    {
        var match = profiles.FirstOrDefault(p => p.Relationship == relationship);
        return match is null
            ? new ProfileResolution.NotFound($"relationship:{relationship}")
            : new ProfileResolution.FamilyMember(match.ProfileId, $"{match.FirstName} {match.LastName}");
    }

    public ProfileResolution MatchByNameInMessage(
        string messageText,
        IReadOnlyList<AccessibleProfileEntry> profiles)
    {
        var normalized = ProfileNormalizer.Normalize(messageText);
        var tokenSet = new HashSet<string>(
            ProfileNormalizer.Tokenize(normalized),
            StringComparer.OrdinalIgnoreCase);

        var matches = new List<AccessibleProfileEntry>();
        foreach (var profile in profiles)
        {
            var normFirst = ProfileNormalizer.Normalize(profile.FirstName);
            var normLast  = ProfileNormalizer.Normalize(profile.LastName);
            var normFull  = $"{normFirst} {normLast}";

            if (tokenSet.Contains(normFirst)
                || tokenSet.Contains(normLast)
                || normalized.Contains(normFull, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(profile);
            }
        }

        return matches.Count == 1
            ? new ProfileResolution.FamilyMember(matches[0].ProfileId, $"{matches[0].FirstName} {matches[0].LastName}")
            : new ProfileResolution.Unresolved();
    }

    public ProfileResolution MatchByHint(
        string hint,
        IReadOnlyList<AccessibleProfileEntry> profiles)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return new ProfileResolution.Unresolved();

        // The AI might output a relationship term in English (e.g. "Father")
        if (Enum.TryParse<RelationshipType>(hint.Trim(), ignoreCase: true, out var rel))
        {
            var relMatch = profiles.FirstOrDefault(p => p.Relationship == rel);
            if (relMatch is not null)
                return new ProfileResolution.FamilyMember(
                    relMatch.ProfileId, $"{relMatch.FirstName} {relMatch.LastName}");
        }

        // Fall back to name matching
        var nameResult = MatchByNameInMessage(hint, profiles);
        return nameResult is ProfileResolution.FamilyMember
            ? nameResult
            : new ProfileResolution.NotFound(hint);
    }
}
