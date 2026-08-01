namespace Rafiq.Application.Features.AiChat.DTOs;

public sealed record HealthSummaryDto(
    string OverallStatus,        // "Good" | "Stable" | "Needs Attention"
    string? OverallStatusNote,   // One short phrase
    List<string> Conditions,
    List<AllergyBriefDto> Allergies,
    MedicationsBriefDto Medications,
    LabResultsBriefDto LabResults,
    List<string> Insights,
    List<string> Recommendations,
    bool HasData
);

public sealed record AllergyBriefDto(string Name, string Severity);
public sealed record MedicationsBriefDto(int Count, bool HasIssues, string? IssueNote);

// Status: "Normal" | "HasAbnormal" | "ReviewRecommended"
public sealed record LabResultsBriefDto(string Status, int AbnormalCount);
