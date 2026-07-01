using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class KnowledgeSource : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public KnowledgeSourceType SourceType { get; set; }
    public string? Url { get; set; }
    public string? Content { get; set; }
    public string? EmbeddingVector { get; set; }
    public ICollection<ChatMessageCitation> Citations { get; set; } = new List<ChatMessageCitation>();
}
