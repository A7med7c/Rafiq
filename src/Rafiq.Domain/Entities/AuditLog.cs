namespace Rafiq.Domain.Entities;

public sealed class AuditLog
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Guid   ActorId     { get; set; }
    public string ActorName   { get; set; } = null!;
    public string ActorEmail  { get; set; } = null!;

    public string Module      { get; set; } = null!;
    public string Action      { get; set; } = null!;
    public string Target      { get; set; } = null!;
    public string Severity    { get; set; } = null!;
    public string Description { get; set; } = null!;

    // Stored as a JSON array: [{"field":"…","before":"…","after":"…"}, …]
    public string? ChangesJson { get; set; }
}
