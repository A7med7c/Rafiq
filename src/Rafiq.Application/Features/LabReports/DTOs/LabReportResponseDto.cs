namespace Rafiq.Application.Features.LabReports.DTOs;

/// <summary>
/// The client-facing response for a Lab Report.
/// Returned by both the upload command and the query handlers.
/// </summary>
public sealed class LabReportResponseDto
{
    public Guid Id { get; init; }

    public string LabName { get; init; } = null!;

    public string? DoctorName { get; init; }

    public string? ReportDate { get; init; }

    public string? OCRText { get; init; }

    /// <summary>
    /// Short patient-friendly AI explanation stored in Description.
    /// </summary>
    public string? Summary { get; init; }

    public string? ImageUrl { get; init; } // Relative URL path

    public DateTime CreatedAt { get; init; }

    public List<LabResultResponseDto> Results { get; init; } = new();
}
