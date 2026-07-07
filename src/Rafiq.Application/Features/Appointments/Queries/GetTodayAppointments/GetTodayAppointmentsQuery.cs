using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Queries.GetTodayAppointments;

public sealed record GetTodayAppointmentsQuery : IRequest<ApiResponse<List<AppointmentResponseDto>>>;
