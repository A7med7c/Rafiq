using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed record CompleteAppointmentCommand(Guid Id) : IRequest<ApiResponse<AppointmentResponseDto>>;
