using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities;

namespace Rafiq.Application.Features.Reviews.Commands.UpdateReviewStatus;

public sealed record UpdateReviewStatusCommand(Guid ReviewId, ReviewStatus Status)
    : IRequest<ApiResponse<bool>>;
