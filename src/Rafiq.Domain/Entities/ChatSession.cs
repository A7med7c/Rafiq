using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities;

public class ChatSession : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string? Title { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
