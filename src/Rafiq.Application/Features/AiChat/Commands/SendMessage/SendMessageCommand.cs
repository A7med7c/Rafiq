using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.AiChat.DTOs;

namespace Rafiq.Application.Features.AiChat.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ConversationId,
    string Text,
    string? Base64Image,
    string? ImageFormat,
    string Language = "en",
    int UtcOffsetMinutes = 0) : IRequest<ApiResponse<AiMessageResponseDto>>;
