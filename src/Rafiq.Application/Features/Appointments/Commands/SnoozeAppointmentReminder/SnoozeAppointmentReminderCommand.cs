using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Appointments.Commands.SnoozeAppointmentReminder;

public sealed record SnoozeAppointmentReminderCommand(Guid AppointmentId, int SnoozeMinutes) : IRequest<ApiResponseBase>;
