using Rafiq.Domain.Common;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Documents;

public class MedicationReminderLog : BaseEntity
{
    protected MedicationReminderLog() { }

    public MedicationReminderLog(
        Guid medicineReminderId,
        Guid userHealthProfileId,
        DateOnly scheduledDate,
        TimeSpan scheduledTime,
        int reminderNumber)
    {
        MedicineReminderId = medicineReminderId;
        UserHealthProfileId = userHealthProfileId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        ReminderNumber = reminderNumber;
        Status = MedicationReminderStatus.Pending;
    }

    public Guid MedicineReminderId { get; private set; }
    public Guid UserHealthProfileId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public TimeSpan ScheduledTime { get; private set; }
    public int ReminderNumber { get; private set; }
    public MedicationReminderStatus Status { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    // Hangfire job ID of the NEXT scheduled reminder — stored so it can be cancelled on confirmation
    public string? NextJobId { get; private set; }

    public virtual MedicineReminder MedicineReminder { get; private set; } = null!;
    public virtual UserHealthProfile UserHealthProfile { get; private set; } = null!;

    public bool IsCompleted =>
        Status is MedicationReminderStatus.Confirmed or MedicationReminderStatus.Cancelled;

    public void MarkAsSent()
    {
        Status = MedicationReminderStatus.Sent;
        SentAt = DateTime.UtcNow;
        MarkUpdated();
    }

    /// <summary>
    /// Records a dose whose time had already passed when it was materialised, so no
    /// reminder was ever sent. Not a completed state — it can still be confirmed late.
    /// </summary>
    public void MarkAsOverdue()
    {
        Status = MedicationReminderStatus.Overdue;
        MarkUpdated();
    }

    public void MarkAsConfirmed()
    {
        Status = MedicationReminderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancel()
    {
        Status = MedicationReminderStatus.Cancelled;
        MarkUpdated();
    }

    public void SetNextJobId(string jobId)
    {
        NextJobId = jobId;
        MarkUpdated();
    }
}
