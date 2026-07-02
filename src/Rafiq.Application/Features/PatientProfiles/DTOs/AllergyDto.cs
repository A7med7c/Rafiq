public sealed record AllergyDto(
    Guid Id,
    string Name,
    string? Reaction,
    string Severity
);