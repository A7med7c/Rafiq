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
    }
}
