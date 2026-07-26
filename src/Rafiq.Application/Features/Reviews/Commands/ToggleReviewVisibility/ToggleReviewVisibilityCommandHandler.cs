using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.ToggleReviewVisibility;

public sealed class ToggleReviewVisibilityCommandHandler
    : IRequestHandler<ToggleReviewVisibilityCommand, ApiResponse<bool>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly ICurrentUserService _currentUser;

    public ToggleReviewVisibilityCommandHandler(
        IAppReviewRepository repo,
        IUnitOfWork uow,
        IAuditLogService audit,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _uow = uow;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<bool>> Handle(
        ToggleReviewVisibilityCommand request, CancellationToken cancellationToken)
    {
        var review = await _repo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return ApiResponse<bool>.FailureResponse("Review not found");

        var before = review.IsVisible ? "Public" : "Hidden";
        review.SetVisible(request.IsVisible);
        await _uow.SaveChangesAsync(cancellationToken);

        if (_currentUser.UserId.HasValue)
        {
            var after  = request.IsVisible ? "Public" : "Hidden";
            var action = request.IsVisible ? "ReviewPublished" : "ReviewHidden";
            await _audit.LogAsync(
                actorId:     _currentUser.UserId.Value,
                actorName:   "Admin",
                actorEmail:  string.Empty,
                module:      "Reviews",
                action:      action,
                target:      $"Review by {review.DisplayName} (#{review.Id.ToString()[..6]})",
                severity:    "Info",
                description: request.IsVisible
                    ? $"Review by '{review.DisplayName}' made public."
                    : $"Review by '{review.DisplayName}' hidden from public feed.",
                changes:     [("Visibility", before, after)],
                cancellationToken: cancellationToken);
        }

        return ApiResponse<bool>.SuccessResponse(true,
            request.IsVisible ? "Review is now visible" : "Review is now hidden");
    }
}
