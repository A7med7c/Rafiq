using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.AiChat.Commands.GenerateConversationTitle;

public sealed record GenerateConversationTitleCommand(Guid ConversationId) : IRequest<ApiResponse<string>>;
