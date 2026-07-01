using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class CaregiverLink : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid CaregiverUserId { get; set; }
    public CaregiverLinkStatus Status { get; set; }
    public PermissionLevel PermissionLevel { get; set; }
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
}
