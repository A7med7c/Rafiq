public sealed record ChronicDiseaseDto(
    Guid Id,
    string Name,
    string Status,
    DateOnly? DiagnosedAt
);