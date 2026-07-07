using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Appointments.Commands.DeleteAppointment;

public sealed record DeleteAppointmentCommand(Guid Id) : IRequest<ApiResponse<bool>>;
