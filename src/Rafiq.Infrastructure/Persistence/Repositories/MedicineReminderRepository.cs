using Microsoft.EntityFrameworkCore;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;

namespace Rafiq.Infrastructure.Persistence.Repositories;

public class MedicineReminderRepository(RafiqDbContext context) : IMedicineReminderRepository
{
    public async Task<MedicineReminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.MedicineReminders
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<List<MedicineReminder>> GetByUserMedicineIdAsync(Guid userMedicineId, CancellationToken cancellationToken = default)
    {
        return await context.MedicineReminders
            .Where(x => x.UserMedicineId == userMedicineId && !x.IsDeleted)
            .OrderBy(x => x.ReminderTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<MedicineReminder> reminders, CancellationToken cancellationToken = default)
    {
        await context.MedicineReminders.AddRangeAsync(reminders, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userMedicineId, 
        TimeSpan time, 
        DateOnly startDate, 
        DateOnly endDate, 
        string repeatType, 
        Guid? excludeId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = context.MedicineReminders.AsQueryable()
            .Where(x => x.UserMedicineId == userMedicineId && 
                        x.ReminderTime == time &&
                        x.StartDate == startDate &&
                        x.EndDate == endDate &&
                        x.RepeatType.ToString() == repeatType &&
                        !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public void Update(MedicineReminder reminder)
    {
        context.MedicineReminders.Update(reminder);
    }

    public void Delete(MedicineReminder reminder)
    {
        reminder.SoftDelete();
        context.MedicineReminders.Update(reminder);
    }
}
