using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.AiChat.Commands.ArchiveConversation;

public sealed record ArchiveConversationCommand(Guid ConversationId) : IRequest<ApiResponse<bool>>;
