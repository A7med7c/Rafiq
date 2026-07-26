using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities;

namespace Rafiq.Application.Features.Reviews.Queries.GetReviewOverview;

public sealed record ReviewOverviewDto(
    int Total,
    int Pending,
    int ThisWeek,
    double AverageStars,
    double HealthScore,
    double PositiveRate,
    int WithReply,
    int FiveStars,
    int FourStars,
    int ThreeStars,
    int TwoStars,
    int OneStar,
    int Visible,
    int Hidden,
    IReadOnlyDictionary<string, int> ByCategory,
    IReadOnlyDictionary<string, int> ByStatus);

public sealed record GetReviewOverviewQuery : IRequest<ApiResponse<ReviewOverviewDto>>;
