using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using AiFeature = Rafiq.Domain.Enums.AiFeature;

namespace Rafiq.Application.Features.AiChat.Commands.SendMessage;

/// <summary>
/// 202-pattern handler: persists the user message + a Pending placeholder for the
/// assistant reply, then enqueues a Hangfire job to run the agent loop in the
/// background. The frontend receives the result via SignalR ("ChatResponse" event)
/// regardless of whether the browser tab is still open when processing completes.
/// </summary>
public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ApiResponse<AiMessageResponseDto>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;
    private readonly IChatBackgroundJobService _chatJobService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiTelemetryContext _telemetryContext;

    public SendMessageCommandHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository,
        IHealthProfileAuthorizationService healthProfileAuthService,
        IChatBackgroundJobService chatJobService,
        IUnitOfWork unitOfWork,
        IAiTelemetryContext telemetryContext)
    {
        _currentUserService = currentUserService;
        _aiConversationRepository = aiConversationRepository;
        _healthProfileAuthService = healthProfileAuthService;
        _chatJobService = chatJobService;
        _unitOfWork = unitOfWork;
        _telemetryContext = telemetryContext;
    }

    public async Task<ApiResponse<AiMessageResponseDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        _telemetryContext.Feature        = AiFeature.Chat;
        _telemetryContext.UserId         = userId;
        _telemetryContext.ConversationId = request.ConversationId;

        var conversation = await _aiConversationRepository.GetWithMessagesAsync(request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        var nextSeq = conversation.Messages.Count > 0
            ? conversation.Messages.Max(m => m.SequenceNumber) + 1
            : 1;

        // Persist the user message immediately so it appears in history right away.
        var userMsg = new AiMessage(conversation.Id, AiMessageRole.User, request.Text, nextSeq);
        await _aiConversationRepository.AddMessageAsync(userMsg, cancellationToken);

        // Persist a Pending placeholder for the assistant reply.
        // Content "…" satisfies the non-empty validation and signals "in progress" to the UI.
        var assistantPlaceholder = new AiMessage(
            conversation.Id, AiMessageRole.Assistant, "…", nextSeq + 1, AiMessageStatus.Pending);
        await _aiConversationRepository.AddMessageAsync(assistantPlaceholder, cancellationToken);

        conversation.MarkMessageActivity(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Enqueue the background job — Hangfire persists it in SQL Server so it
        // survives app restarts and runs even if this HTTP connection closes.
        _chatJobService.EnqueueProcessMessage(
            userId,
            conversation.Id,
            assistantPlaceholder.Id,
            request.Text,
            request.Base64Image,
            request.ImageFormat,
            request.Language,
            request.UtcOffsetMinutes);

        // Return immediately with the placeholder ID so the frontend knows which
        // message will be updated via the "ChatResponse" SignalR event.
        return ApiResponse<AiMessageResponseDto>.SuccessResponse(
            new AiMessageResponseDto(assistantPlaceholder.Id, "…"),
            "Message accepted.");
    }
}
