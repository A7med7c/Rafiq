using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Reviews.Commands.ToggleReviewVisibility;

public sealed record ToggleReviewVisibilityCommand(Guid ReviewId, bool IsVisible)
    : IRequest<ApiResponse<bool>>;
