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
    Overdue = 5,

    /// <summary>
    /// Stage 3 reminder was sent but the patient did not confirm within 15 minutes.
    /// An escalation alert was dispatched to the patient and their emergency contacts.
    /// </summary>
    Missed = 6
}
