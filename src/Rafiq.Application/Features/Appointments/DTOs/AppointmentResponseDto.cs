using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Appointments.DTOs;

public sealed class AppointmentResponseDto
{
    public Guid Id { get; init; }

    public AppointmentType AppointmentType { get; init; }

    public string? CustomType { get; init; }

    public string Title { get; init; } = null!;

    public string Provider { get; init; } = null!;

    public DateTime AppointmentDateTime { get; init; }

    public int? ReminderOffsetMinutes { get; init; }

    public string? Notes { get; init; }

    public AppointmentStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
