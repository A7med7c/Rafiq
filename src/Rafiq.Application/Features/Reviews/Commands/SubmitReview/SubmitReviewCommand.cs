using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Commands.SubmitReview;

public sealed record SubmitReviewCommand(int Stars, string? Comment) : IRequest<ApiResponse<bool>>;
