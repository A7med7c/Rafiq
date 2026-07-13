using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public sealed class MedicationReminderLogRepository(RafiqDbContext context) : IMedicationReminderLogRepository
{
    public async Task AddAsync(MedicationReminderLog log, CancellationToken cancellationToken = default)
    {
        await context.MedicationReminderLogs.AddAsync(log, cancellationToken);
    }

    public async Task<MedicationReminderLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.MedicationReminderLogs
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<MedicationReminderLog?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.MedicationReminderLogs
            .Include(x => x.MedicineReminder)
                .ThenInclude(r => r.UserMedicine)
            .Include(x => x.UserHealthProfile)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsForDateAsync(Guid medicineReminderId, DateOnly date, CancellationToken cancellationToken = default)
    {
        // A Cancelled stage-1 log (e.g. superseded by an edit) does not count as "already
        // scheduled" — otherwise a reminder update could never recreate today's schedule.
        return await context.MedicationReminderLogs
            .AnyAsync(x => x.MedicineReminderId == medicineReminderId
                        && x.ScheduledDate == date
                        && x.ReminderNumber == 1
                        && x.Status != MedicationReminderStatus.Cancelled,
                cancellationToken);
    }

    public async Task<List<MedicationReminderLog>> GetTodayByProfileIdAsync(
        Guid userHealthProfileId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        return await context.MedicationReminderLogs
            .Include(x => x.MedicineReminder)
                .ThenInclude(r => r.UserMedicine)
            .Where(x => x.UserHealthProfileId == userHealthProfileId && x.ScheduledDate == today && !x.IsDeleted)
            .OrderBy(x => x.ScheduledTime)
            .ThenBy(x => x.ReminderNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MedicationReminderLog>> GetPendingOtherLogsAsync(
        Guid medicineReminderId,
        DateOnly date,
        Guid confirmedLogId,
        CancellationToken cancellationToken = default)
    {
        // Fetches every Pending log in the same occurrence that is not the one being confirmed.
        // Sent logs are excluded: their Hangfire jobs have already executed and their notifications
        // have been delivered — there is no job to cancel and no reason to alter their status.
        return await context.MedicationReminderLogs
            .Where(x => x.MedicineReminderId == medicineReminderId
                        && x.ScheduledDate == date
                        && x.Id != confirmedLogId
                        && x.Status == MedicationReminderStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MedicationReminderLog>> GetSentStage3LogsOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        // Confirmed sibling IDs for the same (MedicineReminderId, ScheduledDate) pair.
        var confirmedReminders = context.MedicationReminderLogs
            .Where(l => l.Status == MedicationReminderStatus.Confirmed)
            .Select(l => new { l.MedicineReminderId, l.ScheduledDate });

        return await context.MedicationReminderLogs
            .Include(l => l.MedicineReminder)
                .ThenInclude(r => r.UserMedicine)
            .Include(l => l.UserHealthProfile)
            .Where(l =>
                l.ReminderNumber == 3
                && l.Status == MedicationReminderStatus.Sent
                && l.SentAt <= cutoff
                && !context.MedicationReminderLogs.Any(c =>
                    c.MedicineReminderId == l.MedicineReminderId
                    && c.ScheduledDate == l.ScheduledDate
                    && c.Status == MedicationReminderStatus.Confirmed))
            .ToListAsync(cancellationToken);
    }

    public void Update(MedicationReminderLog log)
    {
        context.MedicationReminderLogs.Update(log);
    }
}
