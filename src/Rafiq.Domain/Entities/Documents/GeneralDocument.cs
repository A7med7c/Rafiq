using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;

public sealed class GeneralDocument : BaseEntity
{
    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string? AiSummary { get; private set; }

    public string ImagePath { get; private set; } = null!;

    public string? DocumentType { get; private set; }

    public string? DoctorName { get; private set; }

    public string? HospitalOrClinic { get; private set; }

    public string? DocumentDate { get; private set; }

    public string? OcrText { get; private set; }

    protected GeneralDocument() { }

    public GeneralDocument(
        Guid userHealthProfileId,
        string title,
        string description,
        string imagePath,
        string? aiSummary = null,
        string? documentType = null,
        string? doctorName = null,
        string? hospitalOrClinic = null,
        string? documentDate = null,
        string? ocrText = null)
    {
        UserHealthProfileId = userHealthProfileId;
        Title = title;
        Description = description;
        ImagePath = imagePath;
        AiSummary = aiSummary;
        DocumentType = documentType;
        DoctorName = doctorName;
        HospitalOrClinic = hospitalOrClinic;
        DocumentDate = documentDate;
        OcrText = ocrText;
    }

    public void Update(
        string title,
        string description,
        string? aiSummary,
        string? imagePath,
        string? documentType = null,
        string? doctorName = null,
        string? hospitalOrClinic = null,
        string? documentDate = null,
        string? ocrText = null)
    {
        Title = title;
        Description = description;
        AiSummary = aiSummary;
        DocumentType = documentType;
        DoctorName = doctorName;
        HospitalOrClinic = hospitalOrClinic;
        DocumentDate = documentDate;
        OcrText = ocrText;
        if (!string.IsNullOrWhiteSpace(imagePath))
            ImagePath = imagePath;

        MarkUpdated();
    }
}