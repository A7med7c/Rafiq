using Rafiq.Application.AI.HealthQuery;

namespace Rafiq.Application.Common.Interfaces;

/// <summary>
/// Safely retrieves the minimum health data needed to answer an already-validated,
/// allowlist-safe <see cref="ParsedHealthQueryIntent"/> for a health profile the caller
/// has already been authorized to read. Never touches anything not named in the intent's
/// fixed category enum.
/// </summary>
public interface IHealthQueryContextBuilder
{
    Task<string> BuildAsync(
        ParsedHealthQueryIntent intent,
        Guid userHealthProfileId,
        CancellationToken cancellationToken = default);
}
