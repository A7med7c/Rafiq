using Rafiq.Domain.Enums;

namespace Rafiq.Application.AI.Models;

public class AiChatMessageDto
{
    public AiMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}