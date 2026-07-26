using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Queries.GetAdminReviews;

public sealed class GetAdminReviewsQueryHandler
    : IRequestHandler<GetAdminReviewsQuery, ApiResponse<AdminReviewsPageDto>>
{
    private readonly IAppReviewRepository _repo;

    public GetAdminReviewsQueryHandler(IAppReviewRepository repo) => _repo = repo;

    public async Task<ApiResponse<AdminReviewsPageDto>> Handle(
        GetAdminReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repo.GetFilteredAsync(
            request.Page, request.PageSize,
            request.Status, request.Category,
            request.MinStars, request.MaxStars,
            request.SortBy, cancellationToken);

        var dtos = items.Select(r => new AdminReviewDto(
            r.Id, r.DisplayName, r.Stars, r.Comment, r.IsVisible,
            r.Status, r.Category, r.AdminNotes, r.AdminReply, r.RepliedAt,
            r.ReviewedAt, r.DeviceInfo, r.AppVersion, r.AppLanguage, r.CreatedAt)).ToList();

        return ApiResponse<AdminReviewsPageDto>.SuccessResponse(
            new AdminReviewsPageDto(dtos, total, request.Page, request.PageSize),
            "Reviews retrieved successfully");
    }
}
