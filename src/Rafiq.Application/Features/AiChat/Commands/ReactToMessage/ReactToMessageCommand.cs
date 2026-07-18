using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.AiChat.Commands.ReactToMessage;

public sealed record ReactToMessageCommand(
    Guid ConversationId,
    Guid MessageId,
    ReactionType ReactionType,
    bool Remove,
    string? Feedback = null
) : IRequest<ApiResponse<bool>>;
