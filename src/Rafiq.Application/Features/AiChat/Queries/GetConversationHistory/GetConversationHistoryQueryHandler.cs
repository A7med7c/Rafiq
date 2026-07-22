using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Queries.GetConversationHistory;

public sealed class GetConversationHistoryQueryHandler
    : IRequestHandler<GetConversationHistoryQuery, ApiResponse<ConversationHistoryDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;

    public GetConversationHistoryQueryHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository,
        IMessageReactionRepository reactionRepository,
        IHealthProfileAuthorizationService healthProfileAuthService)
    {
        _currentUserService       = currentUserService;
        _aiConversationRepository = aiConversationRepository;
        _reactionRepository       = reactionRepository;
        _healthProfileAuthService = healthProfileAuthService;
    }

    public async Task<ApiResponse<ConversationHistoryDto>> Handle(
        GetConversationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var conversation = await _aiConversationRepository.GetWithMessagesAsync(
            request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        var messageIds = conversation.Messages.Select(m => m.Id).ToList();
        var userReactions = await _reactionRepository.GetUserReactionsForMessagesAsync(
            messageIds, userId, cancellationToken);

        // ToolCall / ToolResult messages are internal agent bookkeeping — exclude them
        // from the history that the UI renders so the chat shows only the conversation.
        var dto = new ConversationHistoryDto(
            conversation.Id,
            conversation.Title,
            conversation.LastMessageAt,
            conversation.Messages
                .Where(m => m.Role == AiMessageRole.User || m.Role == AiMessageRole.Assistant)
                .OrderBy(m => m.SequenceNumber)
                .Select(m => new ConversationMessageDto(
                    m.Id,
                    m.Role.ToString(),
                    m.Content,
                    m.SequenceNumber,
                    m.CreatedAt,
                    userReactions.TryGetValue(m.Id, out var reaction) ? reaction.ToString() : null))
                .ToList());

        return ApiResponse<ConversationHistoryDto>.SuccessResponse(dto, "Conversation retrieved successfully.");
    }
}
