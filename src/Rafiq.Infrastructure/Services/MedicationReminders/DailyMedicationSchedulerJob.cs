using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Services.MedicationReminders;

/// <summary>
/// Daily sweep that schedules every active reminder for today. It owns no scheduling logic of its own —
/// it defers to <see cref="IMedicationSchedulingService"/>, the same path the create-reminder flow uses.
/// </summary>
public sealed class DailyMedicationSchedulerJob(
    IMedicineReminderRepository medicineReminderRepository,
    IMedicationSchedulingService schedulingService,
    IDateTimeProvider dateTimeProvider,
    ILogger<DailyMedicationSchedulerJob> logger)
{
    public async Task ScheduleAsync()
    {
        var today = dateTimeProvider.Today;
        var reminders = await medicineReminderRepository.GetActiveForDateAsync(today, CancellationToken.None);

        var recorded = 0;

        foreach (var reminder in reminders)
        {
            if (await schedulingService.ScheduleTodayIfApplicableAsync(reminder, CancellationToken.None))
                recorded++;
        }

        logger.LogInformation(
            "Daily medication sweep for {Date}: recorded {Recorded} of {Total} active reminder(s).",
            today, recorded, reminders.Count);
    }
}
