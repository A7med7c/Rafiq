public sealed class GeneralDocumentPreviewDto
{
    public string? DocumentTitle { get; init; }

    public string? DocumentType { get; init; }

    public string? DoctorName { get; init; }

    public string? HospitalOrClinic { get; init; }

    public string? DocumentDate { get; init; }

    public string? AiSummary { get; init; }

    public string? OcrText { get; init; }

    public string ImagePath { get; init; } = null!;
}