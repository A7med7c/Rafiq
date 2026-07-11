namespace Rafiq.Application.Features.ImagingReports.DTOs;

public sealed class BedrockImagingReportDto
{
    public string? ImagingType { get; set; }

    public string? BodyPart { get; set; }

    public string? Findings { get; set; }

    public string? Impression { get; set; }

    public string? DoctorName { get; set; }

    public string? ReportDate { get; set; }

    public string? OcrText { get; set; }

    public string? AiSummary { get; set; }
}
