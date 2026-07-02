namespace Rafiq.Domain.Entities.Documents;

public class LabReport : MedicalDocument
{
    public string LabName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public ICollection<LabResult> Results { get; set; }
        = new List<LabResult>();
}