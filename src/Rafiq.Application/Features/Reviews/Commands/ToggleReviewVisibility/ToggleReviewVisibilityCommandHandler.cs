using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.ToggleReviewVisibility;

public sealed class ToggleReviewVisibilityCommandHandler
    : IRequestHandler<ToggleReviewVisibilityCommand, ApiResponse<bool>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUnitOfWork _uow;

    public ToggleReviewVisibilityCommandHandler(IAppReviewRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<bool>> Handle(
        ToggleReviewVisibilityCommand request, CancellationToken cancellationToken)
    {
        var review = await _repo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return ApiResponse<bool>.FailureResponse("Review not found");

        review.SetVisible(request.IsVisible);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true,
            request.IsVisible ? "Review is now visible" : "Review is now hidden");
    }
}
