using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Queries.GetReviewStats;

public sealed record ReviewStatsDto(
    int Total,
    int Visible,
    int Hidden,
    double AverageStars,
    int FiveStars,
    int FourStars,
    int ThreeStars,
    int TwoStars,
    int OneStar);

public sealed record GetReviewStatsQuery : IRequest<ApiResponse<ReviewStatsDto>>;
