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
}
