namespace Rafiq.Domain.Enums;

public enum MedicationReminderStatus
{
    Pending = 1,
    Sent = 2,
    Confirmed = 3,
    Cancelled = 4,

    /// <summary>
    /// The dose was planned for today but its time passed before it could be notified,
    /// so no reminder was sent. It still counts against adherence and can still be
    /// confirmed late.
    /// </summary>
    Overdue = 5
}
