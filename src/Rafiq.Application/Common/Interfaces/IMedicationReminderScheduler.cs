namespace Rafiq.Application.Common.Interfaces;

public interface IMedicationReminderScheduler
{
    void CancelJob(string jobId);
}
