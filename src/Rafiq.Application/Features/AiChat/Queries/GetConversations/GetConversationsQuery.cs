using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;

namespace Rafiq.Application.Features.AiChat.Queries.GetConversations;

public sealed record GetConversationsQuery : IRequest<ApiResponse<IReadOnlyList<ConversationSummaryDto>>>;
