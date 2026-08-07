using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.GeneralDocuments.DTOs;

public sealed class GeneralDocumentResponseDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string Description { get; init; } = null!;

    public string? AiSummary { get; init; }

    public string? ImagePath { get; init; }

    public string? DocumentType { get; init; }

    public string? DoctorName { get; init; }

    public string? HospitalOrClinic { get; init; }

    public string? DocumentDate { get; init; }

    public string? OcrText { get; init; }

        public string? MedicalAttentionReason { get; init; }
    public string? RecommendedSpecialty { get; init; }
    public double? ConfidenceScore { get; init; }
    public bool RequiresMedicalAttention { get; init; }
    public string? AttentionLevel { get; init; }

    public DateTime CreatedAt { get; init; }

    public string AnalysisStatus { get; init; } = "Completed";

    public string? FailureReason { get; init; }
}

/// <summary>Returned immediately after async upload — the client uses this ID to track analysis.</summary>
public sealed class UploadGeneralDocumentAsyncResponseDto
{
    public Guid DocumentId { get; init; }
    public string ImagePath { get; init; } = null!;
    public string Title { get; init; } = null!;
}