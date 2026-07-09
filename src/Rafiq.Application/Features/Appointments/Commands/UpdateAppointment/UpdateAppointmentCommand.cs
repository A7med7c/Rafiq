using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Appointments.Commands.UpdateAppointment;

public sealed record UpdateAppointmentCommand(
    Guid Id,
    AppointmentType AppointmentType,
    string? CustomType,
    string Title,
    string Provider,
    DateTime AppointmentDateTime,
    int? ReminderOffsetMinutes,
    string? Notes)
    : IRequest<ApiResponse<AppointmentResponseDto>>;
