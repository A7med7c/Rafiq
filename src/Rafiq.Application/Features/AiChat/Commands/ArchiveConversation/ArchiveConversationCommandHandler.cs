using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Commands.ArchiveConversation;

public sealed class ArchiveConversationCommandHandler
    : IRequestHandler<ArchiveConversationCommand, ApiResponse<bool>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveConversationCommandHandler(
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

    public async Task<ApiResponse<bool>> Handle(ArchiveConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        // Scoped by (id, userId) - a conversation belonging to another user simply
        // doesn't match and comes back as NotFound, never revealing its existence.
        var conversation = await _aiConversationRepository.GetByIdAsync(request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        // Soft delete only: RafiqDbContext.SaveChangesAsync converts the resulting
        // EntityState.Deleted into IsDeleted/DeletedAt on the tracked entity - messages
        // are untouched and remain in the database for historical purposes.
        _aiConversationRepository.Remove(conversation);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Conversation archived successfully.");
    }
}
