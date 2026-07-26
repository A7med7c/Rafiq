using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.ReplyToReview;

public sealed class ReplyToReviewCommandHandler
    : IRequestHandler<ReplyToReviewCommand, ApiResponse<bool>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUserNotificationRepository _notifRepo;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _signalR;
    private readonly IAuditLogService _audit;
    private readonly ICurrentUserService _currentUser;

    public ReplyToReviewCommandHandler(
        IAppReviewRepository repo,
        IUserNotificationRepository notifRepo,
        IUnitOfWork uow,
        INotificationService signalR,
        IAuditLogService audit,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _notifRepo = notifRepo;
        _uow = uow;
        _signalR = signalR;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<bool>> Handle(
        ReplyToReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return ApiResponse<bool>.FailureResponse("Review not found");

        review.SetReply(request.Reply);

        if (review.Status == ReviewStatus.Pending && request.Reply is not null)
            review.UpdateStatus(ReviewStatus.Reviewed);

        // Persist notification to DB so the user sees it even when offline
        if (request.Reply is not null)
        {
            var notification = new UserNotification(
                userId: review.UserId,
                title: "رد على تقييمك",
                body: request.Reply,
                type: "review_reply");

            await _notifRepo.AddAsync(notification, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Also push real-time via SignalR for users who are online
        if (request.Reply is not null)
        {
            await _signalR.SendNotificationToUserAsync(
                review.UserId.ToString(),
                "رد على تقييمك",
                request.Reply,
                cancellationToken);
        }

        if (_currentUser.UserId.HasValue && request.Reply is not null)
        {
            await _audit.LogAsync(
                actorId:     _currentUser.UserId.Value,
                actorName:   "Admin",
                actorEmail:  string.Empty,
                module:      "Reviews",
                action:      "ReviewReplied",
                target:      $"Review by {review.DisplayName} (#{review.Id.ToString()[..6]})",
                severity:    "Info",
                description: $"Admin replied to review by '{review.DisplayName}'.",
                changes:     [("Admin Reply", null, request.Reply)],
                cancellationToken: cancellationToken);
        }

        return ApiResponse<bool>.SuccessResponse(true, "Reply saved");
    }
}
