using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class ImagingReport : BaseEntity
{
    // Required by EF Core for materialisation
    protected ImagingReport() { }

    public ImagingReport(
        Guid userId,
        string imagingType,
        string bodyPart,
        string findings,
        string impression,
        string? doctorName,
        DateOnly reportDate,
        string imageUrl,
        string? ocrText,
        string? description)
    {
        UserId = userId;
        ImagingType = imagingType;
        BodyPart = bodyPart;
        Findings = findings;
        Impression = impression;
        DoctorName = doctorName;
        ReportDate = reportDate;
        ImageUrl = imageUrl;
        OCRText = ocrText;
        Description = description;
    }

    public Guid UserId { get; private set; }

    public string ImagingType { get; set; } = null!;

    public string BodyPart { get; set; } = null!;

    public string Findings { get; set; } = null!;

    public string Impression { get; set; } = null!;

    public string? DoctorName { get; set; }

    public DateOnly ReportDate { get; set; }

    public string ImageUrl { get; private set; } = null!;

    public string? OCRText { get; private set; }

    public string? Description { get; set; } // Stores AI summary
}