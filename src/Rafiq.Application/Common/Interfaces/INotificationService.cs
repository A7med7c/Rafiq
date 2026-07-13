using System.Threading;
using System.Threading.Tasks;

namespace Rafiq.Application.Common.Interfaces
{
    public class MedicationReminderNotificationPayload
    {
        public string ReminderId { get; set; } = string.Empty;
        public string MedicineId { get; set; } = string.Empty;
        public string MedicineName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string ReminderTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string NotificationText { get; set; } = string.Empty;
    }

    public class AppointmentReminderNotificationPayload
    {
        public string AppointmentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string AppointmentDateTime { get; set; } = string.Empty;
        public string NotificationText { get; set; } = string.Empty;
        public string AppointmentType { get; set; } = string.Empty;
        public string? CustomType { get; set; }
    }

    public interface INotificationService
    {
        Task SendNotificationToUserAsync(
            string userId,
            string title,
            string message,
            CancellationToken cancellationToken = default);

        Task SendMedicationReminderAsync(
            string userId,
            MedicationReminderNotificationPayload payload,
            CancellationToken cancellationToken = default);

        Task SendAppointmentReminderAsync(
            string userId,
            AppointmentReminderNotificationPayload payload,
            CancellationToken cancellationToken = default);
    }
}
