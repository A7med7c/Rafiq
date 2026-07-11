using Rafiq.Domain.Enums;

public sealed record CreateAllergyDto(
    string Name,
    AllergySeverity Severity);