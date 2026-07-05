using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class LabReport : BaseEntity
{
    // Required by EF Core for materialisation
    protected LabReport() { }

    public LabReport(
        Guid userId,
        string doctorName,
        string labName,
        DateOnly reportDate,
        string imageUrl,
        string? ocrText,
        string? description)
    {
        UserId = userId;
        DoctorName = doctorName;
        LabName = labName;
        ReportDate = reportDate;
        ImageUrl = imageUrl;
        OCRText = ocrText;
        Description = description;
    }

    public Guid UserId { get; private set; }

    public string DoctorName { get; set; } = null!;

    public string LabName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public string ImageUrl { get; private set; } = null!;

    public string? OCRText { get; private set; }

    public string? Description { get; set; } // Stores AI summary

    public ICollection<LabResult> Results { get; set; }
        = new List<LabResult>();
}