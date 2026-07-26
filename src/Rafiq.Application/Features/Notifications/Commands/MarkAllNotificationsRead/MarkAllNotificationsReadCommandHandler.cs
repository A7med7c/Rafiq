using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, ApiResponse<bool>>
{
    private readonly IUserNotificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public MarkAllNotificationsReadCommandHandler(
        IUserNotificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<bool>> Handle(
        MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return ApiResponse<bool>.FailureResponse("Unauthorized");

        await _repo.MarkAllReadAsync(userId, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
