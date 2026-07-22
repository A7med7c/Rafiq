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
