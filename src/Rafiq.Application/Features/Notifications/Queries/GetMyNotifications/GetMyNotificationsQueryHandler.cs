using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, ApiResponse<GetMyNotificationsResult>>
{
    private readonly IUserNotificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(
        IUserNotificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<GetMyNotificationsResult>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return ApiResponse<GetMyNotificationsResult>.FailureResponse("Unauthorized");

        var items = await _repo.GetForUserAsync(userId, request.Page, request.PageSize, cancellationToken);
        var unreadCount = await _repo.GetUnreadCountAsync(userId, cancellationToken);

        var dtos = items.Select(n => new UserNotificationDto(
            n.Id, n.Title, n.Body, n.TitleAr, n.BodyAr, n.Type, n.IsRead, n.ReadAt, n.CreatedAt
        )).ToList();

        return ApiResponse<GetMyNotificationsResult>.SuccessResponse(
            new GetMyNotificationsResult(dtos, unreadCount, request.Page, request.PageSize));
    }
}
