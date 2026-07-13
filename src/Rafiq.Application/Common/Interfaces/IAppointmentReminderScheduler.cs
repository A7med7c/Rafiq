using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Application.Common.Interfaces;

public interface IAppointmentReminderScheduler
{
    string? ScheduleReminder(Appointment appointment);
    void CancelJob(string? jobId);
}
