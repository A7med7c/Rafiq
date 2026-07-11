namespace Rafiq.Application.Features.AiChat.DTOs;

public sealed record ConversationSummaryDto(
    Guid Id,
    Guid UserHealthProfileId,
    string Title,
    DateTime? LastMessageAt,
    DateTime CreatedAt);
