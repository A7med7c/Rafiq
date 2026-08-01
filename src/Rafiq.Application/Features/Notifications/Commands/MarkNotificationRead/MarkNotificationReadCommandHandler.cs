using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, ApiResponse<bool>>
{
    private readonly IUserNotificationRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
        IUserNotificationRepository repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<bool>> Handle(
        MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return ApiResponse<bool>.FailureResponse("Unauthorized");

        var notification = await _repo.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null || notification.UserId != userId)
            return ApiResponse<bool>.FailureResponse("Not found");

        notification.MarkRead();
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
