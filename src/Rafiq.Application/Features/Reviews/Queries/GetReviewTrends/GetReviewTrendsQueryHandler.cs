using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Queries.GetReviewTrends;

public sealed class GetReviewTrendsQueryHandler
    : IRequestHandler<GetReviewTrendsQuery, ApiResponse<IReadOnlyList<ReviewTrendPointDto>>>
{
    private readonly IAppReviewRepository _repo;

    public GetReviewTrendsQueryHandler(IAppReviewRepository repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<ReviewTrendPointDto>>> Handle(
        GetReviewTrendsQuery request, CancellationToken cancellationToken)
    {
        var points = await _repo.GetTrendsAsync(request.Months, cancellationToken);
        var dtos = points.Select(p => new ReviewTrendPointDto(p.Month, p.AverageStars, p.Count))
                         .ToList();
        return ApiResponse<IReadOnlyList<ReviewTrendPointDto>>.SuccessResponse(dtos, "Trends retrieved");
    }
}
