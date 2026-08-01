namespace Rafiq.Application.Features.Reviews.Queries.GetPublicReviews;

public sealed record PublicReviewDto(
    string DisplayName,
    int Stars,
    string? Comment,
    DateTime CreatedAt);
