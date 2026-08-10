using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Application.Common.Interfaces;

public interface IAppointmentReminderScheduler
{
    string? ScheduleReminder(Appointment appointment);
    string? ScheduleSnoozedReminder(Appointment appointment, int snoozeMinutes);
    void CancelJob(string? jobId);
}
