namespace Rafiq.Application.Features.ImagingReports.DTOs;

public sealed class ImagingReportResponseDto
{
    public Guid ReportId { get; init; }

    public string ImagingType { get; init; } = string.Empty;

    public string BodyPart { get; init; } = string.Empty;

    public string Findings { get; init; } = string.Empty;

    public string Impression { get; init; } = string.Empty;

    public string? DoctorName { get; init; }

    public string? ReportDate { get; init; }

    public string AiSummary { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public string ReportImagePath { get; init; } = string.Empty;
}
