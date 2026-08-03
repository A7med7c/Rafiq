namespace Rafiq.Application.Common.Interfaces;

public interface IMedicationReminderScheduler
{
    void CancelJob(string jobId);

    /// <summary>
    /// Schedules the reminder delivery job for the given log to fire after the
    /// specified delay. Returns the Hangfire job ID so it can be stored for
    /// later cancellation if the user confirms or skips before it fires.
    /// </summary>
    string ScheduleDelayedReminderJob(Guid logId, TimeSpan delay);
}
