using MediatR;
using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.AI.Models;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Commands.SendMessage;

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ApiResponse<AiMessageResponseDto>>
{
    // Classification only ever needs to return the small fixed JSON schema, so it gets a
    // much smaller token budget than the conversational response.
    private const int IntentClassificationMaxTokens = 200;

    // A little recent history helps the classifier resolve follow-up questions
    // ("and my sugar test?") without needing the caller to repeat context.
    private const int IntentClassificationHistoryTurns = 4;

    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;
    private readonly IAiChatService _aiChatService;
    private readonly IHealthQueryContextBuilder _healthQueryContextBuilder;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository,
        IHealthProfileAuthorizationService healthProfileAuthService,
        IAiChatService aiChatService,
        IHealthQueryContextBuilder healthQueryContextBuilder,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _aiConversationRepository = aiConversationRepository;
        _healthProfileAuthService = healthProfileAuthService;
        _aiChatService = aiChatService;
        _healthQueryContextBuilder = healthQueryContextBuilder;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AiMessageResponseDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        // Load as tracked entity (no AsNoTracking) so AiConversation → Modified on save.
        var conversation = await _aiConversationRepository.GetWithMessagesAsync(request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        // Ensure user has access to the health profile. Re-checked on every message,
        // not just at conversation creation, in case access was revoked meanwhile.
        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        // Build history from existing DB-loaded messages BEFORE adding the new user message.
        // This prevents the current user message from appearing twice in the AI context.
        var historyDtos = conversation.Messages
            .OrderBy(m => m.SequenceNumber)
            .Select(m => new AiChatMessageDto { Role = m.Role, Content = m.Content })
            .ToList();

        // Determine the next sequence number.
        var nextSeq = conversation.Messages.Count > 0
            ? conversation.Messages.Max(m => m.SequenceNumber) + 1
            : 1;

        // Understand what the user is actually asking for, then retrieve only the
        // minimum authorized health data needed to answer it.
        var healthContext = await BuildHealthContextAsync(request.Text, historyDtos, conversation.UserHealthProfileId, cancellationToken);

        var aiRequest = new AiChatRequest
        {
            SystemPrompt = HealthAssistantSystemPrompt.Build(),
            HealthContext = healthContext,
            PreviousMessages = historyDtos,
            CurrentUserMessage = request.Text,
            Base64Image = request.Base64Image,
            ImageFormat = request.ImageFormat
        };

        // Call the AI provider.
        var aiResponse = await _aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);

        // Add user message explicitly through the DbSet (not the private-set nav collection).
        var userMsg = new AiMessage(conversation.Id, AiMessageRole.User, request.Text, nextSeq);
        await _aiConversationRepository.AddMessageAsync(userMsg, cancellationToken);

        // Add assistant message explicitly through the DbSet.
        var aiMsg = new AiMessage(conversation.Id, AiMessageRole.Assistant, aiResponse.Content, nextSeq + 1);
        await _aiConversationRepository.AddMessageAsync(aiMsg, cancellationToken);

        // Update LastMessageAt on the tracked conversation entity → EF state: Modified.
        conversation.MarkMessageActivity(DateTime.UtcNow);

        // Single SaveChangesAsync call.
        // Expected EF states before save:
        //   AiConversation  → Modified
        //   userMsg         → Added
        //   aiMsg           → Added
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AiMessageResponseDto>.SuccessResponse(new AiMessageResponseDto(aiResponse.Content), "Message sent successfully.");
    }

    /// <summary>
    /// Classifies the user's message into a validated, allowlist-safe intent, then
    /// retrieves only the minimum authorized health data it names. The AI model never
    /// touches a repository, a database, or any credential - it only ever produces a
    /// small JSON suggestion that is re-validated here before it can influence anything.
    /// Degrades to no health context (rather than failing the whole message) if
    /// classification fails for any reason - the same safe fallback as an off-topic
    /// question that matches no category.
    /// </summary>
    private async Task<string> BuildHealthContextAsync(
        string text,
        IReadOnlyList<AiChatMessageDto> history,
        Guid userHealthProfileId,
        CancellationToken cancellationToken)
    {
        ParsedHealthQueryIntent intent;

        try
        {
            var intentRequest = new AiChatRequest
            {
                SystemPrompt = HealthQueryIntentPrompt.Build(),
                PreviousMessages = history.TakeLast(IntentClassificationHistoryTurns),
                CurrentUserMessage = text,
                MaxOutputTokens = IntentClassificationMaxTokens
            };

            var intentResponse = await _aiChatService.GenerateResponseAsync(intentRequest, cancellationToken);
            intent = HealthQueryIntentParser.Parse(intentResponse.Content);
        }
        catch (ExternalServiceException)
        {
            // Classification is a best-effort understanding step, not a hard dependency -
            // if the AI provider fails here, fall back to no health context rather than
            // failing the user's message entirely.
            intent = ParsedHealthQueryIntent.Empty;
        }

        if (intent.HasNoCategories)
            return string.Empty;

        return await _healthQueryContextBuilder.BuildAsync(intent, userHealthProfileId, cancellationToken);
    }
}
