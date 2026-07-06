using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.DTOs;

public class MedicineReminderResponseDto
{
    public Guid Id { get; set; }
    public Guid UserMedicineId { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string RepeatType { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
