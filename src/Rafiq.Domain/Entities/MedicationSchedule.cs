using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class MedicationSchedule : BaseEntity
{
    public Guid MedicationId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public DoseStatus DoseStatus { get; set; }
    public DateTime? TakenAt { get; set; }
    public string? SkippedReason { get; set; }
    public Medication Medication { get; set; } = null!;
}
