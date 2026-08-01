using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Commands.UpdateReviewCategory;

public sealed class UpdateReviewCategoryCommandHandler
    : IRequestHandler<UpdateReviewCategoryCommand, ApiResponse<bool>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateReviewCategoryCommandHandler(IAppReviewRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateReviewCategoryCommand request, CancellationToken cancellationToken)
    {
        var review = await _repo.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review is null)
            return ApiResponse<bool>.FailureResponse("Review not found");

        review.UpdateCategory(request.Category);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Category updated");
    }
}
