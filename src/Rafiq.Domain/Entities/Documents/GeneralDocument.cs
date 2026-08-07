using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

public sealed class GeneralDocument : BaseEntity
{
    public Guid UserHealthProfileId { get; private set; }

    public UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string? AiSummary { get; private set; }

    public string ImagePath { get; private set; } = null!;

    public string? DocumentType { get; private set; }

    public string? DoctorName { get; private set; }

    public string? HospitalOrClinic { get; private set; }

    public string? DocumentDate { get; private set; }

    public string? OcrText { get; private set; }

    public string? FileHash { get; private set; }

    public string? MedicalAttentionReason { get; private set; }

    public string? RecommendedSpecialty { get; private set; }

    public double? ConfidenceScore { get; private set; }

    // ── Async analysis state ─────────────────────────────────────────────────

    public GeneralDocumentStatus AnalysisStatus { get; private set; } = GeneralDocumentStatus.Pending;

    /// <summary>User-friendly message explaining why analysis failed. Null when not Failed.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>When the background job claimed this document for processing.</summary>
    public DateTime? ProcessingStartedAt { get; private set; }

    protected GeneralDocument() { }

    public GeneralDocument(
        Guid userHealthProfileId,
        string title,
        string description,
        string imagePath,
        string? aiSummary = null,
        string? documentType = null,
        string? doctorName = null,
        string? hospitalOrClinic = null,
        string? documentDate = null,
        string? ocrText = null,
        string? medicalAttentionReason = null,
        string? recommendedSpecialty = null,
        double? confidenceScore = null,
        GeneralDocumentStatus analysisStatus = GeneralDocumentStatus.Pending,
        string? fileHash = null)
    {
        UserHealthProfileId = userHealthProfileId;
        Title = title;
        Description = description;
        ImagePath = imagePath;
        AiSummary = aiSummary;
        DocumentType = documentType;
        DoctorName = doctorName;
        HospitalOrClinic = hospitalOrClinic;
        DocumentDate = documentDate;
        OcrText = ocrText;
        MedicalAttentionReason = medicalAttentionReason;
        RecommendedSpecialty = recommendedSpecialty;
        ConfidenceScore = confidenceScore;
        AnalysisStatus = analysisStatus;
        FileHash = fileHash;
    }

    /// <summary>Called by the background job to claim the document atomically.</summary>
    public void SetProcessing()
    {
        AnalysisStatus = GeneralDocumentStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>Called by the background job on successful AI analysis.</summary>
    public void Complete(
        string title,
        string? aiSummary,
        string? documentType,
        string? doctorName,
        string? hospitalOrClinic,
        string? documentDate,
        string? ocrText,
        string? medicalAttentionReason,
        string? recommendedSpecialty,
        double? confidenceScore)
    {
        Title = title;
        AiSummary = aiSummary;
        DocumentType = documentType;
        DoctorName = doctorName;
        HospitalOrClinic = hospitalOrClinic;
        DocumentDate = documentDate;
        OcrText = ocrText;
        MedicalAttentionReason = medicalAttentionReason;
        RecommendedSpecialty = recommendedSpecialty;
        ConfidenceScore = confidenceScore;
        AnalysisStatus = GeneralDocumentStatus.Completed;
        FailureReason = null;
        MarkUpdated();
    }

    /// <summary>Called by the background job when analysis fails.</summary>
    public void Fail(string reason)
    {
        AnalysisStatus = GeneralDocumentStatus.Failed;
        FailureReason = reason;
        MarkUpdated();
    }

    /// <summary>Reset to Pending for retry — only valid when currently Failed.</summary>
    public void ResetForRetry()
    {
        AnalysisStatus = GeneralDocumentStatus.Pending;
        FailureReason = null;
        ProcessingStartedAt = null;
        MarkUpdated();
    }

    public void Update(
        string title,
        string description,
        string? aiSummary,
        string? imagePath,
        string? documentType = null,
        string? doctorName = null,
        string? hospitalOrClinic = null,
        string? documentDate = null,
        string? ocrText = null)
    {
        Title = title;
        Description = description;
        AiSummary = aiSummary;
        DocumentType = documentType;
        DoctorName = doctorName;
        HospitalOrClinic = hospitalOrClinic;
        DocumentDate = documentDate;
        OcrText = ocrText;
        if (!string.IsNullOrWhiteSpace(imagePath))
            ImagePath = imagePath;

        MarkUpdated();
    }
}
