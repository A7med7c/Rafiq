using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicationReminderEngine.DTOs;

public sealed record MedicationReminderLogDto(
    Guid Id,
    Guid MedicineReminderId,
    Guid UserHealthProfileId,
    string MedicineName,
    string Dosage,
    DateOnly ScheduledDate,
    TimeSpan ScheduledTime,
    int ReminderNumber,
    MedicationReminderStatus Status,
    DateTime? SentAt,
    DateTime? ConfirmedAt,
    DateTime CreatedAt);

public static class MedicationReminderLogMappingExtensions
{
    public static MedicationReminderLogDto ToDto(this MedicationReminderLog log)
        => new(
            log.Id,
            log.MedicineReminderId,
            log.UserHealthProfileId,
            log.MedicineReminder?.UserMedicine?.MedicineName ?? string.Empty,
            log.MedicineReminder?.UserMedicine?.Dosage ?? string.Empty,
            log.ScheduledDate,
            log.ScheduledTime,
            log.ReminderNumber,
            log.Status,
            log.SentAt,
            log.ConfirmedAt,
            log.CreatedAt);
}
