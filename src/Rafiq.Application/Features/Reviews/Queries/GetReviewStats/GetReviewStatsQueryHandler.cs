using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Queries.GetReviewStats;

public sealed class GetReviewStatsQueryHandler
    : IRequestHandler<GetReviewStatsQuery, ApiResponse<ReviewStatsDto>>
{
    private readonly IAppReviewRepository _repo;

    public GetReviewStatsQueryHandler(IAppReviewRepository repo) => _repo = repo;

    public async Task<ApiResponse<ReviewStatsDto>> Handle(
        GetReviewStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _repo.GetStatsAsync(cancellationToken);

        var dto = new ReviewStatsDto(
            stats.Total, stats.Visible, stats.Hidden, stats.AverageStars,
            stats.FiveStars, stats.FourStars, stats.ThreeStars, stats.TwoStars, stats.OneStar);

        return ApiResponse<ReviewStatsDto>.SuccessResponse(dto, "Stats retrieved successfully");
    }
}
