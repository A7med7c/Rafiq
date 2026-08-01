using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities;

namespace Rafiq.Application.Features.Reviews.Queries.GetAdminReviews;

public sealed record AdminReviewDto(
    Guid Id,
    string DisplayName,
    int Stars,
    string? Comment,
    bool IsVisible,
    ReviewStatus Status,
    ReviewCategory Category,
    string? AdminNotes,
    string? AdminReply,
    DateTime? RepliedAt,
    DateTime? ReviewedAt,
    string? DeviceInfo,
    string? AppVersion,
    string? AppLanguage,
    DateTime CreatedAt);

public sealed record AdminReviewsPageDto(
    IReadOnlyList<AdminReviewDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record GetAdminReviewsQuery(
    int Page = 1,
    int PageSize = 20,
    ReviewStatus? Status = null,
    ReviewCategory? Category = null,
    int? MinStars = null,
    int? MaxStars = null,
    string? SortBy = null)
    : IRequest<ApiResponse<AdminReviewsPageDto>>;
