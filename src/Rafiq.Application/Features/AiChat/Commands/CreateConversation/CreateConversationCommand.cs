using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.AiChat.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    Guid UserHealthProfileId,
    string Title) : IRequest<ApiResponse<Guid>>;
