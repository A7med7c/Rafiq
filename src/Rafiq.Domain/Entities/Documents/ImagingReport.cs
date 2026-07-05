namespace Rafiq.Domain.Entities.Documents;

public class ImagingReport
{
    protected ImagingReport() { }

    public ImagingReport(
        Guid userId,
        string reportImagePath)
    {
        ReportId = Guid.NewGuid();
        UserId = userId;
        ReportImagePath = reportImagePath;
    }

    public Guid ReportId { get; private set; }

    public Guid UserId { get; private set; }

    public string ImagingType { get; set; } = null!;

    public string BodyPart { get; set; } = null!;

    public string Findings { get; set; } = null!;

    public string Impression { get; set; } = null!;

    public string DoctorName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public string AiSummary { get; set; } = null!;

    public string ReportImagePath { get; private set; } = null!;
}
