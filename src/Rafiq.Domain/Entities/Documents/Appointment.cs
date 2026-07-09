using Rafiq.Domain.Common;
using Rafiq.Domain.Enums;

namespace Rafiq.Domain.Entities.Documents;

public class Appointment : BaseEntity
{
    protected Appointment() { }

    public Appointment(
        Guid userId,
        AppointmentType appointmentType,
        string? customType,
        string title,
        string provider,
        DateTime appointmentDateTime,
        int? reminderOffsetMinutes,
        string? notes)
    {
        UserId = userId;
        AppointmentType = appointmentType;
        CustomType = appointmentType == AppointmentType.Other ? customType : null;
        Title = title;
        Provider = provider;
        AppointmentDateTime = appointmentDateTime;
        ReminderOffsetMinutes = reminderOffsetMinutes;
        Notes = notes;
        Status = AppointmentStatus.Upcoming;
    }

    public Guid UserId { get; private set; }

    public AppointmentType AppointmentType { get; private set; }

    public string? CustomType { get; private set; }

    public string Title { get; private set; } = null!;

    public string Provider { get; private set; } = null!;

    public DateTime AppointmentDateTime { get; private set; }

    public int? ReminderOffsetMinutes { get; private set; }

    public string? Notes { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public void UpdateDetails(
        AppointmentType appointmentType,
        string? customType,
        string title,
        string provider,
        DateTime appointmentDateTime,
        int? reminderOffsetMinutes,
        string? notes)
    {
        AppointmentType = appointmentType;
        CustomType = appointmentType == AppointmentType.Other ? customType : null;
        Title = title;
        Provider = provider;
        AppointmentDateTime = appointmentDateTime;
        ReminderOffsetMinutes = reminderOffsetMinutes;
        Notes = notes;
    }

    public void MarkAsCompleted()
    {
        if (Status != AppointmentStatus.Upcoming)
            throw new Rafiq.Domain.Exceptions.BadRequestException("Only upcoming appointments can be marked as completed.");

        if (AppointmentDateTime > DateTime.UtcNow)
            throw new Rafiq.Domain.Exceptions.BadRequestException("A future appointment cannot be marked as completed.");

        if (DateTime.UtcNow > AppointmentDateTime.AddHours(3))
            throw new Rafiq.Domain.Exceptions.BadRequestException("The 3-hour completion grace period has expired.");

        Status = AppointmentStatus.Completed;
    }

    public void MarkAsCancelled()
    {
        if (Status != AppointmentStatus.Upcoming)
            throw new Rafiq.Domain.Exceptions.BadRequestException("Only upcoming appointments can be cancelled.");

        Status = AppointmentStatus.Cancelled;
    }

    public void MarkAsMissed()
    {
        if (Status != AppointmentStatus.Upcoming)
            throw new Rafiq.Domain.Exceptions.BadRequestException("Only upcoming appointments can be marked as missed.");

        Status = AppointmentStatus.Missed;
    }
}
