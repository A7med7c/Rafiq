using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicationReminderEngine.DTOs;

public sealed record MedicationReminderLogDto(
    Guid Id,
    Guid MedicineReminderId,
    Guid UserHealthProfileId,
    string MedicineName,
    string Dosage,
    /// <summary>
    /// The wall-clock time the user originally configured for this medication dose.
    /// Sourced from MedicineReminder.ReminderTime — the same for all three attempt logs
    /// that belong to the same occurrence. Use this as the canonical "Dose time" display
    /// value; do not infer it from ReminderNumber.
    /// </summary>
    TimeSpan ReminderTime,
    DateOnly ScheduledDate,
    TimeSpan ScheduledTime,
    int ReminderNumber,
    MedicationReminderStatus Status,
    /// <summary>
    /// True for exactly one log per dose occurrence: the log the user should confirm right now.
    /// Computed server-side so the frontend never needs to infer which attempt is currently valid.
    /// At most one log per (MedicineReminderId, ScheduledDate) group has this set to true.
    /// </summary>
    bool IsActionable,
    DateTime? SentAt,
    DateTime? ConfirmedAt,
    DateTime CreatedAt);

public static class MedicationReminderLogMappingExtensions
{
    public static MedicationReminderLogDto ToDto(this MedicationReminderLog log, bool isActionable = false)
        => new(
            log.Id,
            log.MedicineReminderId,
            log.UserHealthProfileId,
            log.MedicineReminder?.UserMedicine?.MedicineName ?? string.Empty,
            log.MedicineReminder?.UserMedicine?.Dosage ?? string.Empty,
            log.MedicineReminder?.ReminderTime ?? TimeSpan.Zero,
            log.ScheduledDate,
            log.ScheduledTime,
            log.ReminderNumber,
            log.Status,
            isActionable,
            log.SentAt,
            log.ConfirmedAt,
            log.CreatedAt);
}
