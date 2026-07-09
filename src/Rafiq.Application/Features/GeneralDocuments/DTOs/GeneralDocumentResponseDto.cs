namespace Rafiq.Application.Features.GeneralDocuments.DTOs;

public sealed class GeneralDocumentResponseDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string Description { get; init; } = null!;

    public string? AiSummary { get; init; }

    public string? ImagePath { get; init; }

    public DateTime CreatedAt { get; init; }
}