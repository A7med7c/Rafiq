using Rafiq.Application.AI.Resolution;

namespace Rafiq.Application.AI.HealthQuery;

public abstract record QueryScope;

/// <summary>Health data for a single known profile (self or one family member).</summary>
public sealed record SingleProfileScope(Guid ProfileId, string? DisplayName = null) : QueryScope;

/// <summary>
/// Health data spanning all accessible family members (excludes self).
/// Used for FamilyOverview queries and cross-family questions.
/// </summary>
public sealed record FamilyWideScope(IReadOnlyList<AccessibleProfileEntry> Profiles) : QueryScope;

// FUTURE — add when comparison queries are needed:
// public sealed record CompareScope(AccessibleProfileEntry ProfileA, AccessibleProfileEntry ProfileB) : QueryScope;
