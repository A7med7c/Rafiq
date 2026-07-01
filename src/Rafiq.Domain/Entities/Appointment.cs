using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? ProviderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool ReminderSent { get; set; }
    public PatientProfile PatientProfile { get; set; } = null!;
    public HealthcareProvider? Provider { get; set; }
}
