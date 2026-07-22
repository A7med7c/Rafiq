using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.VoiceAgent.DTOs;
using Rafiq.Application.VoiceAgent.Agent;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.VoiceAgent.Commands.ProcessVoiceMessage;

public sealed class ProcessVoiceMessageCommandHandler(
    ICurrentUserService currentUserService,
    IBackgroundUserContext backgroundUserContext,
    IAiConversationRepository conversationRepository,
    VoiceAgentLoop agentLoop,
    INotificationService notificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ProcessVoiceMessageCommand, ApiResponse<VoiceAgentResponseDto>>
{
    public async Task<ApiResponse<VoiceAgentResponseDto>> Handle(
        ProcessVoiceMessageCommand request, CancellationToken cancellationToken)
    {
        // BackgroundUserId is set when running as a fire-and-forget background task
        // (no HttpContext). Populate the scoped IBackgroundUserContext so that all
        // nested command handlers resolved within this scope also get the correct user.
        if (request.BackgroundUserId != Guid.Empty)
            backgroundUserContext.UserId = request.BackgroundUserId;

        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var userIdString = userId.ToString();

        var session = await conversationRepository.GetWithMessagesAsync(
            request.SessionId, userId, cancellationToken)
            ?? throw new NotFoundException("Voice session", request.SessionId);

        var existingHistory = session.Messages
            .OrderBy(m => m.SequenceNumber)
            .Select(m => (m.Role, m.Content))
            .ToList();

        var nowUtc   = DateTime.UtcNow;
        var localNow = nowUtc.AddMinutes(request.UtcOffsetMinutes);
        var ctx = new AgentContext(
            UserId: userId,
            ProfileId: session.UserHealthProfileId,
            SessionId: session.Id,
            Language: request.Language,
            Today: DateOnly.FromDateTime(localNow),   // local date, not UTC
            NowUtc: nowUtc,
            UtcOffsetMinutes: request.UtcOffsetMinutes);

        var userSeq = (session.Messages.Any()
            ? session.Messages.Max(m => m.SequenceNumber)
            : 0) + 1;

        await conversationRepository.AddMessageAsync(
            new AiMessage(session.Id, AiMessageRole.User, request.Text, userSeq),
            cancellationToken);
        session.MarkMessageActivity(DateTime.UtcNow);

        (AgentOutput output, IReadOnlyList<(AiMessageRole Role, string Content)> newMessages) loopResult;

        try
        {
            loopResult = await agentLoop.RunAsync(
                request.Text, existingHistory, ctx, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await TrySendErrorAsync(userIdString, "I encountered an unexpected error. Please try again.", cancellationToken);
            throw;
        }

        var (output, newMessages) = loopResult;

        var nextSeq = userSeq + 1;
        foreach (var (role, content) in newMessages)
        {
            await conversationRepository.AddMessageAsync(
                new AiMessage(session.Id, role, content, nextSeq++),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = output switch
        {
            FinalAnswer fa => new VoiceAgentResponseDto(session.Id, fa.Response, fa.NavigateTo, NeedsMoreInfo: false),
            AskUser au    => new VoiceAgentResponseDto(session.Id, au.Question, NavigateTo: null, NeedsMoreInfo: true),
            _             => null
        };

        if (dto is not null)
        {
            // Push the completed response via SignalR so the frontend receives it regardless
            // of whether this handler was called synchronously or from a background task.
            await TrySendResponseAsync(userIdString, dto, cancellationToken);
            return ApiResponse<VoiceAgentResponseDto>.SuccessResponse(dto);
        }

        await TrySendErrorAsync(userIdString, "Unexpected agent output.", cancellationToken);
        return ApiResponse<VoiceAgentResponseDto>.FailureResponse("Unexpected agent output.");
    }

    private async Task TrySendResponseAsync(string userId, VoiceAgentResponseDto dto, CancellationToken ct)
    {
        try
        {
            await notificationService.SendVoiceAgentResponseAsync(userId, new()
            {
                SessionId    = dto.SessionId,
                Text         = dto.Response,
                NavigateTo   = dto.NavigateTo,
                NeedsMoreInfo = dto.NeedsMoreInfo
            }, ct);
        }
        catch (Exception ex)
        {
            // Notification failure must not fail the whole command.
            _ = ex;
        }
    }

    private async Task TrySendErrorAsync(string userId, string message, CancellationToken ct)
    {
        try
        {
            await notificationService.SendVoiceAgentErrorAsync(userId, new() { Message = message }, ct);
        }
        catch (Exception ex)
        {
            _ = ex;
        }
    }
}
