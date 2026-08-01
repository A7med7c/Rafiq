using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Queries.GetPublicReviews;

public sealed record GetPublicReviewsQuery(int Limit = 20) : IRequest<ApiResponse<IReadOnlyList<PublicReviewDto>>>;
