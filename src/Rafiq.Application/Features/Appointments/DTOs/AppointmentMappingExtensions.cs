using Rafiq.Domain.Entities.Documents;

namespace Rafiq.Application.Features.Appointments.DTOs;

internal static class AppointmentMappingExtensions
{
    public static AppointmentResponseDto ToDto(this Appointment appointment)
        => new()
        {
            Id = appointment.Id,
            AppointmentType = appointment.AppointmentType,
            CustomType = appointment.CustomType,
            Title = appointment.Title,
            Provider = appointment.Provider,
            AppointmentDateTime = appointment.AppointmentDateTime,
            ReminderOffsetMinutes = appointment.ReminderOffsetMinutes,
            Notes = appointment.Notes,
            Status = appointment.Status,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
}
