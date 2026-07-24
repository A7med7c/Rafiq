using MediatR;
using Rafiq.Application.AI.Models;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Chat;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.AiChat.Commands.GenerateConversationTitle;

public sealed class GenerateConversationTitleCommandHandler
    : IRequestHandler<GenerateConversationTitleCommand, ApiResponse<string>>
{
    private const int MaxTitleLength = 60;

    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;
    private readonly IAiChatService _aiChatService;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateConversationTitleCommandHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository,
        IHealthProfileAuthorizationService healthProfileAuthService,
        IAiChatService aiChatService,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _aiConversationRepository = aiConversationRepository;
        _healthProfileAuthService = healthProfileAuthService;
        _aiChatService = aiChatService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<string>> Handle(
        GenerateConversationTitleCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var conversation = await _aiConversationRepository.GetWithMessagesAsync(
            request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        // Need at least one exchange to generate a meaningful title.
        var ordered = conversation.Messages.OrderBy(m => m.SequenceNumber).ToList();
        var firstUser = ordered.FirstOrDefault(m => m.Role == AiMessageRole.User);
        var firstAi   = ordered.FirstOrDefault(m => m.Role == AiMessageRole.Assistant);

        if (firstUser is null)
            return ApiResponse<string>.SuccessResponse(conversation.Title, "No messages yet.");

        // Truncate context to keep the title-generation call cheap.
        var userSnippet = Truncate(firstUser.Content, 300);
        var aiSnippet   = firstAi is not null ? Truncate(firstAi.Content, 300) : string.Empty;

        var prompt = string.IsNullOrEmpty(aiSnippet)
            ? $"User asked: {userSnippet}"
            : $"User asked: {userSnippet}\n\nAssistant replied: {aiSnippet}";

        var aiRequest = new AiChatRequest
        {
            SystemPrompt =
                "You are a conversation title generator. " +
                "Given the first exchange in a health-assistant chat, produce a concise title of 3–6 words. " +
                "Match the language of the user's message (Arabic or English). " +
                "Reply with ONLY the title — no quotes, no punctuation at the end, no explanation.",
            HealthContext = string.Empty,
            PreviousMessages = [],
            CurrentUserMessage = prompt,
            MaxOutputTokens = 30,
        };

        var response = await _aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);

        var title = response.Content.Trim().Trim('"', '\'');
        if (title.Length > MaxTitleLength)
            title = title[..MaxTitleLength].TrimEnd() + "…";

        if (string.IsNullOrWhiteSpace(title))
            return ApiResponse<string>.SuccessResponse(conversation.Title, "Title unchanged.");

        conversation.UpdateTitle(title);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.SuccessResponse(title, "Title generated.");
    }

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max] + "…";
}
