using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Entities.Documents;

public class ImagingReport : BaseEntity
{
    // Required by EF Core for materialisation
    protected ImagingReport() { }

    public ImagingReport(
        Guid userHealthProfileId,
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
        UserHealthProfileId = userHealthProfileId;
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

    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

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