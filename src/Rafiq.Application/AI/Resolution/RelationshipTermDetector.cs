using Rafiq.Domain.Enums;

namespace Rafiq.Application.AI.Resolution;

/// <summary>
/// Deterministically detects relationship terms (e.g. "mother", "بابا") in a message
/// before any AI call. Returns a confident result only when exactly ONE relationship type
/// is found, preventing ambiguity from being silently resolved.
/// Dictionary keys are pre-normalized (alef→ا, ة→ه, ى→ي, no tashkeel, lowercase).
/// </summary>
public static class RelationshipTermDetector
{
    private static readonly Dictionary<string, RelationshipType> TermMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ── English ──────────────────────────────────────────────────────────
            { "mother",      RelationshipType.Mother      },
            { "mom",         RelationshipType.Mother      },
            { "mum",         RelationshipType.Mother      },
            { "father",      RelationshipType.Father      },
            { "dad",         RelationshipType.Father      },
            { "papa",        RelationshipType.Father      },
            { "son",         RelationshipType.Son         },
            { "daughter",    RelationshipType.Daughter    },
            { "brother",     RelationshipType.Brother     },
            { "sister",      RelationshipType.Sister      },
            { "wife",        RelationshipType.Wife        },
            { "husband",     RelationshipType.Husband     },
            { "grandfather", RelationshipType.Grandfather },
            { "grandpa",     RelationshipType.Grandfather },
            { "grandmother", RelationshipType.Grandmother },
            { "grandma",     RelationshipType.Grandmother },
            { "granny",      RelationshipType.Grandmother },

            // ── Arabic (pre-normalized) ───────────────────────────────────────
            // Mother
            { "ماما",    RelationshipType.Mother },
            { "امي",     RelationshipType.Mother },
            { "امه",     RelationshipType.Mother },
            { "والدتي",  RelationshipType.Mother },
            { "والدته",  RelationshipType.Mother },
            // Father
            { "بابا",    RelationshipType.Father },
            { "ابي",     RelationshipType.Father },
            { "ابوه",    RelationshipType.Father },
            { "ابويا",   RelationshipType.Father },
            { "والدي",   RelationshipType.Father },
            { "والده",   RelationshipType.Father },
            // Son
            { "ابني",    RelationshipType.Son },
            { "ابنه",    RelationshipType.Son },
            { "ولدي",    RelationshipType.Son },
            { "ولده",    RelationshipType.Son },
            // Daughter
            { "بنتي",    RelationshipType.Daughter },
            { "بنته",    RelationshipType.Daughter },
            { "ابنتي",   RelationshipType.Daughter },
            { "ابنته",   RelationshipType.Daughter },
            // Brother
            { "اخي",     RelationshipType.Brother },
            { "اخويا",   RelationshipType.Brother },
            { "اخوه",    RelationshipType.Brother },
            // Sister
            { "اختي",    RelationshipType.Sister },
            { "اخته",    RelationshipType.Sister },
            // Wife
            { "زوجتي",   RelationshipType.Wife },
            { "مراتي",   RelationshipType.Wife },
            { "زوجته",   RelationshipType.Wife },
            { "مراته",   RelationshipType.Wife },
            // Husband
            { "زوجي",    RelationshipType.Husband },
            { "جوزي",    RelationshipType.Husband },
            { "راجلي",   RelationshipType.Husband },
            // Grandfather
            { "جدي",     RelationshipType.Grandfather },
            { "جده",     RelationshipType.Grandfather },
            // Grandmother
            { "جدتي",    RelationshipType.Grandmother },
            { "جدته",    RelationshipType.Grandmother },
            { "تيتا",    RelationshipType.Grandmother },
            { "نينا",    RelationshipType.Grandmother },
        };

    /// <summary>
    /// Returns a confident match only when exactly one relationship type appears in the
    /// message. Multiple different relationship terms → not confident (caller falls through
    /// to next resolution stage).
    /// </summary>
    public static DetectionResult TryDetect(string message)
    {
        var normalized = ProfileNormalizer.Normalize(message);
        var tokens = ProfileNormalizer.Tokenize(normalized);
        var matches = new HashSet<RelationshipType>();

        foreach (var token in tokens)
        {
            if (TermMap.TryGetValue(token, out var rel))
                matches.Add(rel);
        }

        return matches.Count == 1
            ? DetectionResult.Confident(matches.Single())
            : DetectionResult.NotDetected();
    }
}

public sealed record DetectionResult(bool IsConfident, RelationshipType? Relationship)
{
    public static DetectionResult Confident(RelationshipType rel) => new(true, rel);
    public static DetectionResult NotDetected() => new(false, null);
}
