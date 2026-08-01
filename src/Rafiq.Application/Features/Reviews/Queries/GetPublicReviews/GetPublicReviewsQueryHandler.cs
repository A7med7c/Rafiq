using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Queries.GetPublicReviews;

public sealed class GetPublicReviewsQueryHandler
    : IRequestHandler<GetPublicReviewsQuery, ApiResponse<IReadOnlyList<PublicReviewDto>>>
{
    private readonly IAppReviewRepository _reviewRepository;

    public GetPublicReviewsQueryHandler(IAppReviewRepository reviewRepository)
        => _reviewRepository = reviewRepository;

    public async Task<ApiResponse<IReadOnlyList<PublicReviewDto>>> Handle(
        GetPublicReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetVisibleAsync(request.Limit, cancellationToken);

        var dtos = reviews
            .Select(r => new PublicReviewDto(r.DisplayName, r.Stars, r.Comment, r.CreatedAt))
            .ToList();

        return ApiResponse<IReadOnlyList<PublicReviewDto>>.SuccessResponse(dtos);
    }
}
