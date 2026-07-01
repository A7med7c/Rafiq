using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces;

public interface IAuditableRequest
{
    AuditAction AuditAction { get; }
    string EntityType { get; }
    Guid? EntityId { get; }
    Guid? PatientProfileId { get; }
}
