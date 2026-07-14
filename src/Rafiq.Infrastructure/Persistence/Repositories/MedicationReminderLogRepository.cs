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
            .Where(x => x.UserHealthProfileId == userHealthProfileId
                        && x.ScheduledDate == today
                        && !x.IsDeleted
                        && x.Status != MedicationReminderStatus.Cancelled)
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
        return await context.MedicationReminderLogs
            .Where(x => x.MedicineReminderId == medicineReminderId
                        && x.ScheduledDate == date
                        && x.Id != confirmedLogId
                        && x.Status == MedicationReminderStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MedicationReminderLog>> GetPendingAndOverdueLogsAsync(
        Guid medicineReminderId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await context.MedicationReminderLogs
            .Where(x => x.MedicineReminderId == medicineReminderId
                        && x.ScheduledDate == date
                        && (x.Status == MedicationReminderStatus.Pending
                            || x.Status == MedicationReminderStatus.Overdue))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MedicationReminderLog>> GetSentStage3LogsOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        return await context.MedicationReminderLogs
            .Include(x => x.MedicineReminder)
                .ThenInclude(r => r.UserMedicine)
            .Include(x => x.UserHealthProfile)
            .Where(x => x.ReminderNumber == 3
                        && x.Status == MedicationReminderStatus.Sent
                        && x.SentAt != null
                        && x.SentAt < cutoff
                        && !x.IsDeleted
                        && !context.MedicationReminderLogs.Any(other =>
                            other.MedicineReminderId == x.MedicineReminderId
                            && other.ScheduledDate == x.ScheduledDate
                            && other.Status == MedicationReminderStatus.Confirmed))
            .ToListAsync(cancellationToken);
    }

    public void Update(MedicationReminderLog log)
    {
        context.MedicationReminderLogs.Update(log);
    }
}
