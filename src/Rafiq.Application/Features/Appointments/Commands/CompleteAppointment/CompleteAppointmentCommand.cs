using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed record CompleteAppointmentCommand(Guid Id) : IRequest<ApiResponse<bool>>;
