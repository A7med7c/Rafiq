using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Queries.GetConversations;

public sealed class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, ApiResponse<IReadOnlyList<ConversationSummaryDto>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;

    public GetConversationsQueryHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository)
    {
        _currentUserService = currentUserService;
        _aiConversationRepository = aiConversationRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<ConversationSummaryDto>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var conversations = await _aiConversationRepository.GetAllByUserIdAsync(userId, cancellationToken);

        var dtos = conversations
            .Select(c => new ConversationSummaryDto(c.Id, c.UserHealthProfileId, c.Title, c.LastMessageAt, c.CreatedAt))
            .ToList();

        return ApiResponse<IReadOnlyList<ConversationSummaryDto>>.SuccessResponse(dtos, "Conversations retrieved successfully.");
    }
}
