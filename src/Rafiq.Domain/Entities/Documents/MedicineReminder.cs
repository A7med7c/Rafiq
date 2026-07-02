using Rafiq.Domain.Common;

namespace Rafiq.Domain.Entities.Documents;

public class MedicineReminder : BaseEntity
{
    public Guid MedicineId { get; set; }

    public TimeOnly ReminderTime { get; set; }

    public string RepeatType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; }

    public Medicine Medicine { get; set; } = null!;
}