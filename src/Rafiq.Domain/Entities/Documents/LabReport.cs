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
        string? description,
        string? fileHash = null)
    {
        UserHealthProfileId = userHealthProfileId;
        DoctorName = doctorName;
        LabName = labName;
        ReportDate = reportDate;
        ImageUrl = imageUrl;
        OCRText = ocrText;
        Description = description;
        FileHash = fileHash;
    }

    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string DoctorName { get; set; } = null!;

    public string LabName { get; set; } = null!;

    public DateOnly ReportDate { get; set; }

    public string ImageUrl { get; private set; } = null!;

    public string? OCRText { get; private set; }

    public string? Description { get; set; } // Stores AI summary

    public string? FileHash { get; private set; }

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

    public void SyncResults(IEnumerable<LabResult> newResults)
    {
        // 1. Remove results that are no longer in the list (matching by TestName)
        var toRemove = Results
            .Where(r => !newResults.Any(nr => string.Equals(nr.TestName, r.TestName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var r in toRemove)
        {
            Results.Remove(r);
        }

        // 2. Add or update the results
        foreach (var nr in newResults)
        {
            var existing = Results.FirstOrDefault(r => string.Equals(r.TestName, nr.TestName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Value = nr.Value;
                existing.Unit = nr.Unit;
                existing.NormalRange = nr.NormalRange;
                existing.Status = nr.Status;
            }
            else
            {
                Results.Add(nr);
            }
        }
    }
}