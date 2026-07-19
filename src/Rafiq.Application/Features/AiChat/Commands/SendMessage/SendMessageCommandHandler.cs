using MediatR;
using Rafiq.Application.AI.HealthQuery;
using Rafiq.Application.AI.Models;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.AI.Resolution;
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

    // Enough history for the classifier to resolve follow-ups like "and her last lab?"
    // while staying well within the cheap classification call's context window.
    private const int IntentClassificationHistoryTurns = 8;

    private readonly ICurrentUserService _currentUserService;
    private readonly IAiConversationRepository _aiConversationRepository;
    private readonly IHealthProfileAccessRepository _healthProfileAccessRepository;
    private readonly IHealthProfileAuthorizationService _healthProfileAuthService;
    private readonly IAiChatService _aiChatService;
    private readonly IHealthQueryContextBuilder _healthQueryContextBuilder;
    private readonly IFamilyProfileResolver _familyProfileResolver;
    private readonly IConversationStateCache _conversationStateCache;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        ICurrentUserService currentUserService,
        IAiConversationRepository aiConversationRepository,
        IHealthProfileAccessRepository healthProfileAccessRepository,
        IHealthProfileAuthorizationService healthProfileAuthService,
        IAiChatService aiChatService,
        IHealthQueryContextBuilder healthQueryContextBuilder,
        IFamilyProfileResolver familyProfileResolver,
        IConversationStateCache conversationStateCache,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _aiConversationRepository = aiConversationRepository;
        _healthProfileAccessRepository = healthProfileAccessRepository;
        _healthProfileAuthService = healthProfileAuthService;
        _aiChatService = aiChatService;
        _healthQueryContextBuilder = healthQueryContextBuilder;
        _familyProfileResolver = familyProfileResolver;
        _conversationStateCache = conversationStateCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AiMessageResponseDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        // Load as tracked entity so AiConversation → Modified on save.
        var conversation = await _aiConversationRepository.GetWithMessagesAsync(request.ConversationId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(AiConversation), request.ConversationId);

        // Re-checked on every message in case access was revoked since the conversation started.
        await _healthProfileAuthService.EnsureCanReadAsync(conversation.UserHealthProfileId, cancellationToken);

        // Build history from DB-loaded messages BEFORE adding the new user message.
        var historyDtos = conversation.Messages
            .OrderBy(m => m.SequenceNumber)
            .Select(m => new AiChatMessageDto { Role = m.Role, Content = m.Content })
            .ToList();

        var nextSeq = conversation.Messages.Count > 0
            ? conversation.Messages.Max(m => m.SequenceNumber) + 1
            : 1;

        // Load accessible family profiles once — one DB call shared across all pipeline stages.
        var accessEntries = await LoadAccessibleProfileEntriesAsync(userId, cancellationToken);

        var healthContext = await BuildHealthContextAsync(
            request.Text,
            historyDtos,
            conversation.UserHealthProfileId,
            conversation.Id,
            accessEntries,
            cancellationToken);

        var aiRequest = new AiChatRequest
        {
            SystemPrompt = HealthAssistantSystemPrompt.Build(),
            HealthContext = healthContext,
            PreviousMessages = historyDtos,
            CurrentUserMessage = request.Text,
            Base64Image = request.Base64Image,
            ImageFormat = request.ImageFormat
        };

        var aiResponse = await _aiChatService.GenerateResponseAsync(aiRequest, cancellationToken);

        var userMsg = new AiMessage(conversation.Id, AiMessageRole.User, request.Text, nextSeq);
        await _aiConversationRepository.AddMessageAsync(userMsg, cancellationToken);

        var aiMsg = new AiMessage(conversation.Id, AiMessageRole.Assistant, aiResponse.Content, nextSeq + 1);
        await _aiConversationRepository.AddMessageAsync(aiMsg, cancellationToken);

        conversation.MarkMessageActivity(DateTime.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AiMessageResponseDto>.SuccessResponse(
            new AiMessageResponseDto(aiMsg.Id, aiResponse.Content), "Message sent successfully.");
    }

    /// <summary>
    /// Loads all active accessible profiles for the current user (one DB call) and maps
    /// them into resolver-friendly entries. Self-access records are excluded — the user's
    /// own profile is always available via conversation.UserHealthProfileId.
    /// </summary>
    private async Task<IReadOnlyList<AccessibleProfileEntry>> LoadAccessibleProfileEntriesAsync(
        Guid userId, CancellationToken ct)
    {
        var accesses = await _healthProfileAccessRepository.GetActiveAccessibleProfilesAsync(userId, ct);

        return accesses
            .Where(a => a.Relationship != RelationshipType.Self)
            .Select(a => new AccessibleProfileEntry(
                a.UserHealthProfileId,
                a.UserHealthProfile.FirstName,
                a.UserHealthProfile.LastName,
                a.Relationship))
            .ToList();
    }

    /// <summary>
    /// 4-stage deterministic-first resolution pipeline followed by AI intent classification.
    /// The AI is called once per message for categories/operations regardless of resolution
    /// stage reached. The <c>targetProfile</c> field is only added to the AI prompt when
    /// deterministic resolution has failed (saves tokens in the common case).
    /// </summary>
    private async Task<string> BuildHealthContextAsync(
        string text,
        IReadOnlyList<AiChatMessageDto> history,
        Guid ownProfileId,
        Guid conversationId,
        IReadOnlyList<AccessibleProfileEntry> accessEntries,
        CancellationToken ct)
    {
        // ── Stage 1: Deterministic relationship detection ────────────────────
        ProfileResolution resolution = new ProfileResolution.Unresolved();

        var detection = RelationshipTermDetector.TryDetect(text);
        if (detection.IsConfident)
        {
            resolution = _familyProfileResolver.MatchByRelationship(detection.Relationship!.Value, accessEntries);
        }

        // ── Stage 1.5: Self-reference detection ──────────────────────────────
        if (resolution is ProfileResolution.Unresolved && SelfReferenceDetector.IsConfident(text))
        {
            resolution = new ProfileResolution.Self(ownProfileId);
            _conversationStateCache.ClearState(conversationId);
        }

        // ── Stage 2: Name scan ───────────────────────────────────────────────
        if (resolution is ProfileResolution.Unresolved)
        {
            resolution = _familyProfileResolver.MatchByNameInMessage(text, accessEntries);
        }

        // ── Stage 3: In-memory conversation state ────────────────────────────
        if (resolution is ProfileResolution.Unresolved)
        {
            var cached = _conversationStateCache.GetState(conversationId);
            if (cached is not null)
                resolution = new ProfileResolution.FamilyMember(cached.ProfileId, cached.DisplayName);
        }

        // ── Stage 4: AI intent classification ────────────────────────────────
        // Always call AI for categories/operations.
        // Only include targetProfile in schema when deterministic resolution has failed.
        bool profileAlreadyResolved = resolution is ProfileResolution.Self or ProfileResolution.FamilyMember;

        ParsedHealthQueryIntent intent;
        try
        {
            var intentRequest = new AiChatRequest
            {
                SystemPrompt = HealthQueryIntentPrompt.Build(includeTargetProfile: !profileAlreadyResolved),
                PreviousMessages = history.TakeLast(IntentClassificationHistoryTurns),
                CurrentUserMessage = text,
                MaxOutputTokens = IntentClassificationMaxTokens
            };

            var intentResponse = await _aiChatService.GenerateResponseAsync(intentRequest, ct);
            intent = HealthQueryIntentParser.Parse(intentResponse.Content);
        }
        catch (ExternalServiceException)
        {
            // Classification is best-effort — degrade to no health context rather than
            // failing the user's message entirely.
            intent = ParsedHealthQueryIntent.Empty;
        }

        // If still unresolved and AI provided a name hint, try to resolve from it.
        if (resolution is ProfileResolution.Unresolved && intent.TargetProfileHint is not null)
        {
            resolution = _familyProfileResolver.MatchByHint(intent.TargetProfileHint, accessEntries);
        }

        // Final fallback: unresolved with no hint → treat as self.
        if (resolution is ProfileResolution.Unresolved)
            resolution = new ProfileResolution.Self(ownProfileId);

        // ── Update conversation state ─────────────────────────────────────────
        if (resolution is ProfileResolution.FamilyMember fm)
            _conversationStateCache.SetState(conversationId, fm.ProfileId, fm.DisplayName);
        else
            _conversationStateCache.ClearState(conversationId);

        if (intent.HasNoCategories)
            return string.Empty;

        // ── Build scope and load context ──────────────────────────────────────
        switch (resolution)
        {
            case ProfileResolution.NotFound notFound:
                // Person was named but not found in accessible profiles.
                // Return a directive context — the AI will inform the user without loading data.
                return $"[RESOLUTION: The user asked about '{notFound.HintUsed}' but no accessible " +
                       "family member matches that reference. Do not load or invent any health data. " +
                       "Politely inform the user in their language that this person could not be found " +
                       "in their accessible family profiles, and suggest they check Family Profiles in Rafiq.]";

            case ProfileResolution.FamilyMember familyMember:
                // Re-verify access in case it was revoked since the initial check.
                await _healthProfileAuthService.EnsureCanReadAsync(familyMember.ProfileId, ct);
                var familyScope = intent.Categories.Contains(HealthQueryCategory.FamilyOverview)
                    ? (QueryScope)new FamilyWideScope(accessEntries)
                    : new SingleProfileScope(familyMember.ProfileId, familyMember.DisplayName);
                return await _healthQueryContextBuilder.BuildAsync(intent, familyScope, ct);

            default: // Self
                var selfProfileId = ((ProfileResolution.Self)resolution).ProfileId;
                var selfScope = intent.Categories.Contains(HealthQueryCategory.FamilyOverview)
                    ? (QueryScope)new FamilyWideScope(accessEntries)
                    : new SingleProfileScope(selfProfileId);
                return await _healthQueryContextBuilder.BuildAsync(intent, selfScope, ct);
        }
    }
}
