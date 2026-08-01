namespace Rafiq.Domain.Enums;

public enum GeneralDocumentStatus
{
    /// <summary>Document uploaded and queued for AI analysis.</summary>
    Pending = 0,

    /// <summary>Background job is actively running AI analysis.</summary>
    Processing = 1,

    /// <summary>AI analysis completed successfully.</summary>
    Completed = 2,

    /// <summary>AI analysis failed; FailureReason contains the user-friendly message.</summary>
    Failed = 3,
}
