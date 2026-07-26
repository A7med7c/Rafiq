using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Commands.ReplyToReview;

public sealed record ReplyToReviewCommand(Guid ReviewId, string? Reply)
    : IRequest<ApiResponse<bool>>;
