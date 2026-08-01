using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler
    : IRequestHandler<DeleteReviewCommand, ApiResponse<bool>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteReviewCommandHandler(IAppReviewRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return ApiResponse<bool>.FailureResponse("Review not found");

        _repo.Remove(review);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Review deleted successfully");
    }
}
