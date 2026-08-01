using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.UpdateReviewStatus;

public sealed class UpdateReviewStatusCommandHandler
    : IRequestHandler<UpdateReviewStatusCommand, ApiResponse<bool>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly ICurrentUserService _currentUser;

    public UpdateReviewStatusCommandHandler(
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
        UpdateReviewStatusCommand request, CancellationToken cancellationToken)
    {
        var review = await _repo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return ApiResponse<bool>.FailureResponse("Review not found");

        var before = review.Status.ToString();
        review.UpdateStatus(request.Status);
        await _uow.SaveChangesAsync(cancellationToken);

        if (_currentUser.UserId.HasValue)
        {
            await _audit.LogAsync(
                actorId:     _currentUser.UserId.Value,
                actorName:   "Admin",
                actorEmail:  string.Empty,
                module:      "Reviews",
                action:      "ReviewStatusUpdated",
                target:      $"Review by {review.DisplayName} (#{review.Id.ToString()[..6]})",
                severity:    "Info",
                description: $"Review status changed from '{before}' to '{request.Status}'.",
                changes:     [("Status", before, request.Status.ToString())],
                cancellationToken: cancellationToken);
        }

        return ApiResponse<bool>.SuccessResponse(true, "Status updated");
    }
}
