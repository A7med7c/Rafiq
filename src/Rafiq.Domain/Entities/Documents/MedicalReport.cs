namespace Rafiq.Domain.Entities.Documents;

public class MedicalReport : MedicalDocument
{
    public string DoctorName { get; set; } = null!;

    public string ReportTitle { get; set; } = null!;

    public string Diagnosis { get; set; } = null!;

    public string Recommendations { get; set; } = null!;

    public DateOnly ReportDate { get; set; }
}