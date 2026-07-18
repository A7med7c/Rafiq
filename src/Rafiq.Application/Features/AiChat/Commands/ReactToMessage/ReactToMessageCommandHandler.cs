using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Commands.ReactToMessage;

public sealed class ReactToMessageCommandHandler
    : IRequestHandler<ReactToMessageCommand, ApiResponse<bool>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;

    public ReactToMessageCommandHandler(
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

    public async Task<ApiResponse<bool>> Handle(ReactToMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var conversation = await _aiConversationRepository.GetByIdAsync(
            request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        if (request.Remove)
            await _reactionRepository.RemoveAsync(request.MessageId, userId, request.ReactionType, cancellationToken);
        else
            await _reactionRepository.UpsertAsync(request.MessageId, userId, request.ReactionType, request.Feedback, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
