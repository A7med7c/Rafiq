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
        string? notes,
        AppointmentStatus status)
    {
        AppointmentType = appointmentType;
        CustomType = appointmentType == AppointmentType.Other ? customType : null;
        Title = title;
        Provider = provider;
        AppointmentDateTime = appointmentDateTime;
        ReminderOffsetMinutes = reminderOffsetMinutes;
        Notes = notes;
        Status = status;
    }
}
