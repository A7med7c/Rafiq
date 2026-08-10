using Hangfire;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;

namespace Rafiq.Infrastructure.Services.AppointmentReminders;

public sealed class AppointmentReminderScheduler(
    IBackgroundJobClient backgroundJobClient,
    IDateTimeProvider dateTimeProvider)
    : IAppointmentReminderScheduler
{
    public string? ScheduleReminder(Appointment appointment)
    {
        if (appointment.ReminderOffsetMinutes is null)
            return null;

        if (appointment.Status != AppointmentStatus.Upcoming)
            return null;

        // AppointmentDateTime is stored without timezone info (local wall-clock time as the
        // user entered it). Compare against DateTime.Now (also local) so the delay is always
        // correct regardless of whether the server runs in UTC or a different timezone.
        var reminderTime = appointment.AppointmentDateTime.AddMinutes(-appointment.ReminderOffsetMinutes.Value);
        var delay = reminderTime - DateTime.UtcNow;

        if (delay <= TimeSpan.Zero)
        {
            Console.WriteLine($"[DEBUG] AppointmentReminderScheduler: delay is {delay}, returning null.");
            return null;
        }

        Console.WriteLine($"[DEBUG] AppointmentReminderScheduler: Scheduling job for {delay.TotalMinutes} minutes from now.");

        return backgroundJobClient.Schedule<AppointmentReminderJob>(
            job => job.ExecuteAsync(appointment.Id),
            delay);
    }

    public void CancelJob(string? jobId)
    {
        if (!string.IsNullOrEmpty(jobId))
            backgroundJobClient.Delete(jobId);
    }

    public string? ScheduleSnoozedReminder(Appointment appointment, int snoozeMinutes)
    {
        if (appointment.Status != AppointmentStatus.Upcoming)
            return null;

        var delay = TimeSpan.FromMinutes(snoozeMinutes);

        Console.WriteLine($"[DEBUG] AppointmentReminderScheduler: Scheduling snoozed job for {delay.TotalMinutes} minutes from now.");

        return backgroundJobClient.Schedule<AppointmentReminderJob>(
            job => job.ExecuteAsync(appointment.Id),
            delay);
    }
}
