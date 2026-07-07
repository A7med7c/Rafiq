using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Queries.GetUpcomingAppointments;

public sealed record GetUpcomingAppointmentsQuery : IRequest<ApiResponse<List<AppointmentResponseDto>>>;
