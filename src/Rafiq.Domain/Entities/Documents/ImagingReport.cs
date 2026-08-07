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
        string? description,
        string? medicalAttentionReason = null,
        string? recommendedSpecialty = null,
        double? confidenceScore = null,
        string? fileHash = null)
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
        MedicalAttentionReason = medicalAttentionReason;
        RecommendedSpecialty = recommendedSpecialty;
        ConfidenceScore = confidenceScore;
        FileHash = fileHash;
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

    public string? FileHash { get; private set; }

    public string? MedicalAttentionReason { get; private set; }

    public string? RecommendedSpecialty { get; private set; }

    public double? ConfidenceScore { get; private set; }
    
    public void Update(
        string imagingType,
        string bodyPart,
        string findings,
        string impression,
        string? doctorName,
        DateOnly reportDate,
        string? description,
        string? medicalAttentionReason,
        string? recommendedSpecialty,
        double? confidenceScore,
        string? imageUrl,
        string? ocrText)
    {
        ImagingType = imagingType;
        BodyPart = bodyPart;
        Findings = findings;
        Impression = impression;
        DoctorName = doctorName;
        ReportDate = reportDate;
        Description = description;
        MedicalAttentionReason = medicalAttentionReason;
        RecommendedSpecialty = recommendedSpecialty;
        ConfidenceScore = confidenceScore;
        if (!string.IsNullOrWhiteSpace(imageUrl))
            ImageUrl = imageUrl;
        OCRText = ocrText;
        MarkUpdated();
    }
}