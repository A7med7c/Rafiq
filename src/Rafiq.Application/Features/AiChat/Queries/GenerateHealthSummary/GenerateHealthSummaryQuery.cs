using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;

namespace Rafiq.Application.Features.AiChat.Queries.GenerateHealthSummary;

public sealed record GenerateHealthSummaryQuery(Guid UserHealthProfileId)
    : IRequest<ApiResponse<HealthSummaryDto>>;
