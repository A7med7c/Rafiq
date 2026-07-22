using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.VoiceAgent.Commands.CreateVoiceSession;

public sealed class CreateVoiceSessionCommandHandler(
    ICurrentUserService currentUserService,
    IAiConversationRepository conversationRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVoiceSessionCommand, ApiResponse<Guid>>
{
    public async Task<ApiResponse<Guid>> Handle(
        CreateVoiceSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var session = new AiConversation(
            userId,
            request.ProfileId,
            $"Voice Session {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            AiConversationSource.Voice);

        await conversationRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(session.Id, "Voice session created.");
    }
}
