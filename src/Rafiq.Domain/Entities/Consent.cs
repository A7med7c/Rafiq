using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class Consent : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public ConsentType ConsentType { get; set; }
    public ConsentStatus Status { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
}
