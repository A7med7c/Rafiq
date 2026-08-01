using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Queries.GetReviewTrends;

public sealed record ReviewTrendPointDto(string Month, double AverageStars, int Count);

public sealed record GetReviewTrendsQuery(int Months = 6)
    : IRequest<ApiResponse<IReadOnlyList<ReviewTrendPointDto>>>;
