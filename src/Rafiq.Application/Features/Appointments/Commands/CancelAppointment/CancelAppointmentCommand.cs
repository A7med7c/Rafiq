using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid Id) : IRequest<ApiResponse<bool>>;
