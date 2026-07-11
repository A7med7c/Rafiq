using System.Collections.Generic;

namespace Rafiq.Application.AI.Models;

public class AiChatRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string HealthContext { get; set; } = string.Empty;
    public IEnumerable<AiChatMessageDto> PreviousMessages { get; set; } = new List<AiChatMessageDto>();
    public string CurrentUserMessage { get; set; } = string.Empty;
    public string? Base64Image { get; set; }
    public string? ImageFormat { get; set; }

    /// <summary>Overrides the configured default max output tokens for this call only (e.g. a smaller budget for JSON-only classification calls). Null uses the configured default.</summary>
    public int? MaxOutputTokens { get; set; }
}