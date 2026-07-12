using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;

namespace Rafiq.Application.Features.AiChat.Commands.RenameConversation;

public sealed record RenameConversationCommand(
    Guid ConversationId,
    string Title) : IRequest<ApiResponse<ConversationSummaryDto>>;
