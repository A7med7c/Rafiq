using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Queries.GetAdminReviews;

public sealed record AdminReviewDto(
    Guid Id,
    string DisplayName,
    int Stars,
    string? Comment,
    bool IsVisible,
    DateTime CreatedAt);

public sealed record AdminReviewsPageDto(
    IReadOnlyList<AdminReviewDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record GetAdminReviewsQuery(int Page = 1, int PageSize = 20)
    : IRequest<ApiResponse<AdminReviewsPageDto>>;
