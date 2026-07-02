namespace Rafiq.Domain.Entities.Documents;

public class ImagingReport : MedicalDocument
{
    public string ImagingType { get; set; } = null!;

    public string BodyPart { get; set; } = null!;

    public string Findings { get; set; } = null!;

    public string Impression { get; set; } = null!;

    public DateOnly ReportDate { get; set; }
}