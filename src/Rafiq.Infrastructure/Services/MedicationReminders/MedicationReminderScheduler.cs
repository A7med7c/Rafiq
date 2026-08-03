using Hangfire;
using Rafiq.Application.Common.Interfaces;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

public sealed class MedicationReminderScheduler(IBackgroundJobClient backgroundJobClient)
    : IMedicationReminderScheduler
{
    public void CancelJob(string jobId)
    {
        backgroundJobClient.Delete(jobId);
    }

    public string ScheduleDelayedReminderJob(Guid logId, TimeSpan delay)
    {
        return backgroundJobClient.Schedule<MedicationReminderJob>(
            job => job.ExecuteAsync(logId),
            delay);
    }
}
