namespace Rafiq.Application.Features.AiChat.DTOs;

public sealed record ConversationHistoryDto(
    Guid Id,
    string Title,
    DateTime? LastMessageAt,
    IReadOnlyList<ConversationMessageDto> Messages);

public sealed record ConversationMessageDto(
    Guid Id,
    string Role,
    string Content,
    int SequenceNumber,
    DateTime CreatedAt,
    string? UserReaction = null);
