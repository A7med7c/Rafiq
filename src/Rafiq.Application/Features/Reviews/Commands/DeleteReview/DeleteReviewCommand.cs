using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Commands.DeleteReview;

public sealed record DeleteReviewCommand(Guid ReviewId) : IRequest<ApiResponse<bool>>;
