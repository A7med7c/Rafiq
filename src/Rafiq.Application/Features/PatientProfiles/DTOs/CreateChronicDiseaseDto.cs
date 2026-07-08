using Rafiq.Domain.Enums;

public sealed record CreateChronicDiseaseDto(
    string Name,
    DateOnly? DiagnosedAt,
    DiseaseStatus Status);