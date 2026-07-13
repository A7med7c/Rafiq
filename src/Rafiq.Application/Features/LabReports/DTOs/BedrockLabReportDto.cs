namespace Rafiq.Application.Features.LabReports.DTOs;

/// <summary>
/// The shape of the JSON that Bedrock returns for a lab report.
/// Property names match the JSON keys defined in LabReportPrompt.
/// </summary>
public sealed class BedrockLabReportDto
{
    public bool IsValidDocument { get; set; }

    public string? DetectedDocumentType { get; set; }

    public string? LabName { get; set; }

    public string? DoctorName { get; set; }

    public string? ReportDate { get; set; }

    public string? OcrText { get; set; }

    public string? Summary { get; set; }

    public List<BedrockLabResultDto> Tests { get; set; } = new();
}
