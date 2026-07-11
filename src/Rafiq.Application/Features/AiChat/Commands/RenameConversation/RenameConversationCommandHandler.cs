using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Commands.RenameConversation;

public sealed class RenameConversationCommandHandler
    : IRequestHandler<RenameConversationCommand, ApiResponse<ConversationSummaryDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;
    private readonly IUnitOfWork _unitOfWork;

    public RenameConversationCommandHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository,
        IHealthProfileAuthorizationService healthProfileAuthService,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _aiConversationRepository = aiConversationRepository;
        _healthProfileAuthService = healthProfileAuthService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ConversationSummaryDto>> Handle(
        RenameConversationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        // Scoped by (id, userId) - a conversation belonging to another user simply
        // doesn't match and comes back as NotFound, never revealing its existence.
        var conversation = await _aiConversationRepository.GetByIdAsync(request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        conversation.UpdateTitle(request.Title);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ConversationSummaryDto(
            conversation.Id,
            conversation.UserHealthProfileId,
            conversation.Title,
            conversation.LastMessageAt,
            conversation.CreatedAt);

        return ApiResponse<ConversationSummaryDto>.SuccessResponse(dto, "Conversation renamed successfully.");
    }
}
