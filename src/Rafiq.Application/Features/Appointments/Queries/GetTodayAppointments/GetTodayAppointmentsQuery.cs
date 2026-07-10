using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Queries.GetTodayAppointments;

public sealed record GetTodayAppointmentsQuery(Guid ProfileId) : IRequest<ApiResponse<List<AppointmentResponseDto>>>;
