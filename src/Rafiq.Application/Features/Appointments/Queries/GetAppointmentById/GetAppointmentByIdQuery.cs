using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;

namespace Rafiq.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed record GetAppointmentByIdQuery(Guid Id) : IRequest<ApiResponse<AppointmentResponseDto>>;
