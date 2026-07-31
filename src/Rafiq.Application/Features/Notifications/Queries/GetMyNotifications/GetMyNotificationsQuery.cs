using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Notifications.Queries.GetMyNotifications;

public record UserNotificationDto(
    Guid Id,
    string Title,
    string Body,
    string? TitleAr,
    string? BodyAr,
    string Type,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public record GetMyNotificationsQuery(int Page = 1, int PageSize = 30)
    : IRequest<ApiResponse<GetMyNotificationsResult>>;

public record GetMyNotificationsResult(
    IReadOnlyList<UserNotificationDto> Items,
    int UnreadCount,
    int Page,
    int PageSize);
