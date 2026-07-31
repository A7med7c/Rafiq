namespace Rafiq.Application.Features.Admin;

public sealed record AdminDashboardDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalProfiles,
    int ManagedProfiles,
    int AppointmentsToday,
    int AppointmentsThisMonth,
    int PendingAppointments,
    int CompletedAppointments,
    int MedicationRemindersToday,
    int MedicalDocuments,
    int AiConversations,
    int NewRegistrationsThisMonth,
    decimal MonthlyGrowthPercent,
    IReadOnlyList<AdminTrendPointDto> UserGrowth,
    IReadOnlyList<AdminTrendPointDto> AppointmentTrend,
    IReadOnlyList<AdminDistributionItemDto> GenderDistribution,
    IReadOnlyList<AdminRecentUserDto> RecentUsers,
    IReadOnlyList<AdminRecentAppointmentDto> RecentAppointments);

public sealed record AdminTrendPointDto(string Label, int Value);

public sealed record AdminDistributionItemDto(string Label, int Value);

public sealed record AdminRecentUserDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    string? ProfileImageUrl);

public sealed record AdminRecentAppointmentDto(
    Guid Id,
    string Title,
    string Provider,
    string PatientName,
    DateTime AppointmentDateTime,
    string Status);

public sealed class AdminUserQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Role { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record AdminUserListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    DateTime CreatedAt,
    string? ProfileImageUrl,
    bool HasHealthProfile);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// ── Audit Logs ─────────────────────────────────────────────────────────────

public sealed record AuditChangeDto(string Field, string? Before, string? After);

public sealed record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    string ActorName,
    string ActorEmail,
    string Module,
    string Action,
    string Target,
    string Severity,
    string Description,
    IReadOnlyList<AuditChangeDto> Changes);

public sealed class AuditLogQuery
{
    public string? Search    { get; init; }
    public string? Module    { get; init; }
    public string? Severity  { get; init; }
    public string? DateFrom  { get; init; }
    public string? DateTo    { get; init; }
    public int Page          { get; init; } = 1;
    public int PageSize      { get; init; } = 10;
}

// ── AI Operations ──────────────────────────────────────────────────────────

// Overview

/// <summary>Per-day quality snapshot for the 7-day trend chart.</summary>
public sealed record AiQualityDayDto(
    string Date,
    int    Requests,
    int    Failed,
    int    ThumbsUp,
    int    ThumbsDown);

/// <summary>
/// Composite AI health overview. HealthScore is derived:
///   successRate7d × 0.65 + positiveRate × 0.35
/// </summary>
public sealed record AiOverviewDto(
    int     AiHealthScore,           // 0-100 composite quality signal
    decimal PositiveRatePercent,     // thumbs-up / total reactions
    int     UnreviewedNegative,      // thumbs-down with TriageStatus = New
    int     FailedLast7Days,
    int     AvgResponseTimeMs,
    int     RequestsToday,
    int     TotalChatConversations,
    int     TotalVoiceSessions,
    IReadOnlyList<AiQualityDayDto> QualityTrend);

// Conversations

public sealed class AiRequestQuery
{
    public string?   Search        { get; init; }
    public string?   Feature       { get; init; }
    public string?   Status        { get; init; } // "success" | "failed"
    public DateTime? From          { get; init; }
    public DateTime? To            { get; init; }
    public string    SortBy        { get; init; } = "createdAt";
    public string    SortDirection { get; init; } = "desc";
    public int       Page          { get; init; } = 1;
    public int       PageSize      { get; init; } = 20;
}

public sealed record AiRequestListItemDto(
    Guid     Id,
    Guid?    UserId,
    string?  UserName,
    string?  UserEmail,
    string   Feature,
    string   ModelName,
    bool     Success,
    string?  ErrorType,
    int      DurationMs,
    DateTime CreatedAt);

/// <summary>Detail payload for the conversations drawer.</summary>
public sealed record AiRequestDetailDto(
    Guid     Id,
    string   Feature,
    string   ModelName,
    bool     Success,
    string?  ErrorType,
    int      DurationMs,
    DateTime CreatedAt,
    Guid?    UserId,
    string?  UserName,
    string?  UserEmail,
    Guid?    ConversationId,
    string?  UserPrompt,   // last user message in the conversation
    string?  AiResponse);  // last assistant message in the conversation

// Feedback

public sealed class AiFeedbackQuery
{
    public string?   Search    { get; init; }
    public string?   Reaction  { get; init; } // "ThumbsUp" | "ThumbsDown"
    public string?   Status    { get; init; } // "New" | "Investigating" | "Resolved" | "Ignored"
    public string?   Category  { get; init; }
    public DateTime? DateFrom  { get; init; }
    public DateTime? DateTo    { get; init; }
    public int       Page      { get; init; } = 1;
    public int       PageSize  { get; init; } = 20;
}

/// <summary>AI quality insights derived from the last 30 days of telemetry and feedback.</summary>
public sealed record AiInsightsDto(
    string? MostUsedFeature,
    int     MostUsedFeatureCount,
    string? HighestErrorFeature,
    int     HighestErrorCount,
    string? SlowestFeature,
    int     SlowestFeatureAvgMs,
    string? FastestFeature,
    int     FastestFeatureAvgMs,
    string? MostReportedCategory,
    int     MostReportedCategoryCount);

public sealed record AiFeedbackListItemDto(
    Guid     Id,
    Guid     UserId,
    string   UserName,
    string   UserEmail,
    string   Reaction,       // "ThumbsUp" | "ThumbsDown"
    string?  UserComment,    // free-text comment the user wrote
    string?  Category,       // predefined category string
    string   TriageStatus,   // "New" | "Investigating" | "Resolved" | "Ignored"
    string?  AdminNotes,
    string   MessageExcerpt, // first ~80 chars — compact row preview
    string   MessageContent, // full AI message content — shown when expanded
    string?  UserPrompt,     // the user question that preceded this AI response
    Guid     ConversationId,
    DateTime CreatedAt);

public sealed record UpdateAiFeedbackDto(
    string  TriageStatus,
    string? Category,
    string? AdminNotes);

// Performance

public sealed record AiDailyStatDto(
    string Date,
    int    Requests,
    int    Failed,
    int    AvgDurationMs);

public sealed record AiPerformanceDto(
    IReadOnlyList<AiDailyStatDto>           Daily,
    IReadOnlyList<AdminDistributionItemDto> RequestsByFeature,
    int     SlowRequests,
    int     P95LatencyMs,
    int     AvgLatencyMs,
    decimal ErrorRatePercent);

// ── Usage Intelligence ──────────────────────────────────────────────────────

public sealed record UsageIntelligenceOverviewDto(
    int TotalAiRequests,
    int FlaggedRequests,
    int UsersNeedingReview,
    int WarningsSent);

public sealed record UsageAttentionUserDto(
    Guid     UserId,
    string   UserName,
    string   UserEmail,
    string?  ProfileImageUrl,
    int      TotalRequests,
    int      FlaggedRequests,
    int      WarningsSent,
    DateTime? LastActivity);

public sealed record UsageFlaggedRequestDto(
    Guid     Id,
    string   RequestType,
    string   UserRequest,
    string   AiResponse,
    string   Classification,
    string   Reason,
    DateTime CreatedAt);

public sealed record UsageAdminActionDto(
    Guid     Id,
    string   ActionType,
    string   AdminName,
    string?  Notes,
    DateTime CreatedAt);

public sealed record UsageUserDetailDto(
    Guid                              UserId,
    string                            UserName,
    string                            UserEmail,
    string?                           ProfileImageUrl,
    int                               TotalRequests,
    int                               FlaggedRequests,
    int                               WarningsSent,
    DateTime?                         LastActivity,
    bool                              IsAiRestricted,
    bool                              IsRestricted,
    bool                              IsSuspended,
    IReadOnlyList<UsageFlaggedRequestDto> FlaggedItems,
    IReadOnlyList<UsageAdminActionDto>    ActionHistory);

public sealed class UsageAttentionQueueQuery
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record TakeUsageActionDto(
    string  ActionType,
    string? Notes);
