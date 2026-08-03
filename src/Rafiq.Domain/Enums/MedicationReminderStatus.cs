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
    Missed = 6,

    /// <summary>
    /// The patient explicitly chose to skip this dose. Counts against adherence.
    /// This is a terminal state — the occurrence will not reappear as active.
    /// </summary>
    Skipped = 7,

    /// <summary>
    /// The patient postponed this dose reminder. A new notification will fire after
    /// the configured snooze interval. This is NOT a terminal state — the log will
    /// transition back to Sent when the snooze job fires.
    /// </summary>
    Snoozed = 8
}
