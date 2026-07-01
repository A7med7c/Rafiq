using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class ChatMessageCitation : BaseEntity
{
    public Guid ChatMessageId { get; set; }
    public Guid KnowledgeSourceId { get; set; }
    public string ClaimText { get; set; } = string.Empty;
    public string Locator { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public ChatMessage ChatMessage { get; set; } = null!;
    public KnowledgeSource KnowledgeSource { get; set; } = null!;
}
