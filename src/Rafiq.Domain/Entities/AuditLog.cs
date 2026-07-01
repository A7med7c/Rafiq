using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class AuditLog : BaseEntity
{
    private AuditLog() { }

    public AuditLog(Guid actorUserId, Guid? patientProfileId, AuditAction action, string entityType, Guid entityId, string? ipAddress)
    {
        ActorUserId = actorUserId;
        PatientProfileId = patientProfileId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        IpAddress = ipAddress;
        Timestamp = DateTime.UtcNow;
    }

    public Guid ActorUserId { get; private set; }
    public Guid? PatientProfileId { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; }
    public PatientProfile? PatientProfile { get; private set; }

    public override void SoftDelete()
    {
        throw new NotSupportedException("Audit logs cannot be deleted.");
    }
}
