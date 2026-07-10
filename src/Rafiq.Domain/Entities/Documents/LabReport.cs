using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;

namespace Rafiq.Domain.Entities.Documents;

public class LabReport : BaseEntity
{
    // Required by EF Core for materialisation
    protected LabReport() { }

    public LabReport(
        Guid userHealthProfileId,
        string doctorName,
        string labName,
        DateOnly reportDate,
        string imageUrl,
        string? ocrText,
        string? description)
    {
        UserHealthProfileId = userHealthProfileId;
        DoctorName = doctorName;
        LabName = labName;
        ReportDate = reportDate;
        ImageUrl = imageUrl;
        OCRText = ocrText;
        Description = description;
    }

    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string DoctorName { get; set; } = null!;

    public string LabName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public string ImageUrl { get; private set; } = null!;

    public string? OCRText { get; private set; }

    public string? Description { get; set; } // Stores AI summary

    public ICollection<LabResult> Results { get; set; }
        = new List<LabResult>();
    
    public void Update(
        string doctorName,
        string labName,
        DateOnly reportDate,
        string? description,
        string? imageUrl,
        string? ocrText)
    {
        DoctorName = doctorName;
        LabName = labName;
        ReportDate = reportDate;
        Description = description;
        if (!string.IsNullOrWhiteSpace(imageUrl))
            ImageUrl = imageUrl;
        OCRText = ocrText;
        MarkUpdated();
    }
}