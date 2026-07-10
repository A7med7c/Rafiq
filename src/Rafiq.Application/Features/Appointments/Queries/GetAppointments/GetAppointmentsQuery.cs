using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Queries.GetAppointments;

public sealed record GetAppointmentsQuery(Guid ProfileId) : IRequest<ApiResponse<List<AppointmentResponseDto>>>;
