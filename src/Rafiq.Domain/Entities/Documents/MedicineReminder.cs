using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Documents;

public class MedicineReminder : BaseEntity
{
    // Required by EF Core
    protected MedicineReminder() { }

    public MedicineReminder(
        Guid userMedicineId,
        TimeSpan reminderTime,
        DateOnly startDate,
        DateOnly endDate,
        RepeatType repeatType)
    {
        UserMedicineId = userMedicineId;
        ReminderTime = reminderTime;
        StartDate = startDate;
        EndDate = endDate;
        RepeatType = repeatType;
        IsEnabled = true;
    }

    public Guid UserMedicineId { get; private set; }
    
    public TimeSpan ReminderTime { get; set; }
    
    public DateOnly StartDate { get; set; }
    
    public DateOnly EndDate { get; set; }
    
    public RepeatType RepeatType { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public DateTime? LastTriggeredAt { get; set; }

    public virtual UserMedicine UserMedicine { get; private set; } = null!;

    public void ToggleStatus()
    {
        IsEnabled = !IsEnabled;
        MarkUpdated();
    }

    public void UpdateDetails(TimeSpan reminderTime, DateOnly startDate, DateOnly endDate, RepeatType repeatType)
    {
        ReminderTime = reminderTime;
        StartDate = startDate;
        EndDate = endDate;
        RepeatType = repeatType;
        MarkUpdated();
    }

    public void RecordTrigger()
    {
        LastTriggeredAt = DateTime.UtcNow;
        MarkUpdated();
    }
}
