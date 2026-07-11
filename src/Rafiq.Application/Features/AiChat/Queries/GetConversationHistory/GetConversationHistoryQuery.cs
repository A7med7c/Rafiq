using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;

namespace Rafiq.Application.Features.AiChat.Queries.GetConversationHistory;

public sealed record GetConversationHistoryQuery(Guid ConversationId)
    : IRequest<ApiResponse<ConversationHistoryDto>>;
