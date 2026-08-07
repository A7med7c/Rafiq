namespace Rafiq.Application.Features.ImagingReports.DTOs;

public sealed class ImagingReportResponseDto
{
    public Guid Id { get; init; }

    public string ImagingType { get; init; } = null!;

    public string BodyPart { get; init; } = null!;

    public string Findings { get; init; } = null!;

    public string Impression { get; init; } = null!;

    public string? DoctorName { get; init; }

    public string? ReportDate { get; init; }

    public string? ImageUrl { get; init; }

    public string? OCRText { get; init; }

    public string? Summary { get; init; } // AI Explanation

        public string? MedicalAttentionReason { get; init; }
    public string? RecommendedSpecialty { get; init; }
    public double? ConfidenceScore { get; init; }
    public bool RequiresMedicalAttention { get; init; }
    public string? AttentionLevel { get; init; }

    public DateTime CreatedAt { get; init; }
}
