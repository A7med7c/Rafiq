using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities;

namespace Rafiq.Application.Features.Reviews.Commands.UpdateReviewCategory;

public sealed record UpdateReviewCategoryCommand(Guid ReviewId, ReviewCategory Category)
    : IRequest<ApiResponse<bool>>;
