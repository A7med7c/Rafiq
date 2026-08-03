using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.SnoozeMedicationReminder;

public sealed record SnoozeMedicationReminderCommand(Guid ReminderLogId, int SnoozeMinutes)
    : IRequest<ApiResponseBase>;
