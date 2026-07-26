using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Queries.GetReviewOverview;

public sealed class GetReviewOverviewQueryHandler
    : IRequestHandler<GetReviewOverviewQuery, ApiResponse<ReviewOverviewDto>>
{
    private readonly IAppReviewRepository _repo;

    public GetReviewOverviewQueryHandler(IAppReviewRepository repo) => _repo = repo;

    public async Task<ApiResponse<ReviewOverviewDto>> Handle(
        GetReviewOverviewQuery request, CancellationToken cancellationToken)
    {
        var o = await _repo.GetOverviewAsync(cancellationToken);

        var dto = new ReviewOverviewDto(
            o.Total, o.Pending, o.ThisWeek, o.AverageStars,
            o.HealthScore, o.PositiveRate, o.WithReply,
            o.FiveStars, o.FourStars, o.ThreeStars, o.TwoStars, o.OneStar,
            o.Visible, o.Hidden,
            ByCategory: o.ByCategory.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            ByStatus:   o.ByStatus.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

        return ApiResponse<ReviewOverviewDto>.SuccessResponse(dto, "Overview retrieved");
    }
}
