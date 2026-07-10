using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Queries.GetUpcomingAppointments;

public sealed record GetUpcomingAppointmentsQuery(Guid ProfileId) : IRequest<ApiResponse<List<AppointmentResponseDto>>>;
