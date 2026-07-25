using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Reviews.Queries.GetAdminReviews;

public sealed class GetAdminReviewsQueryHandler
    : IRequestHandler<GetAdminReviewsQuery, ApiResponse<AdminReviewsPageDto>>
{
    private readonly IAppReviewRepository _repo;
    private readonly IUnitOfWork _uow;

    public GetAdminReviewsQueryHandler(IAppReviewRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<AdminReviewsPageDto>> Handle(
        GetAdminReviewsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repo.GetAllAsync(request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(r => new AdminReviewDto(
            r.Id, r.DisplayName, r.Stars, r.Comment, r.IsVisible, r.CreatedAt)).ToList();

        return ApiResponse<AdminReviewsPageDto>.SuccessResponse(
            new AdminReviewsPageDto(dtos, total, request.Page, request.PageSize),
            "Reviews retrieved successfully");
    }
}
