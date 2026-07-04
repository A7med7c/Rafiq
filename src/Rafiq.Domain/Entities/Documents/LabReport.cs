namespace Rafiq.Domain.Entities.Documents;

public class LabReport : MedicalDocument
{
    // Required by EF Core for materialisation
    protected LabReport() { }

    public LabReport(
        Guid userId,
        Guid documentTypeId,
        string title,
        byte[] imageData,
        string? description,
        string? ocrText)
    {
        UserId = userId;
        DocumentTypeId = documentTypeId;
        Title = title;
        ImageData = imageData;
        Description = description;
        OCRText = ocrText;
    }

    public string DoctorName { get; set; } = null!;

    public string LabName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public ICollection<LabResult> Results { get; set; }
        = new List<LabResult>();
}