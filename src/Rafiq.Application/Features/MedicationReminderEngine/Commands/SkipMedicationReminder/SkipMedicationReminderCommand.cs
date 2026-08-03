using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.SkipMedicationReminder;

public sealed record SkipMedicationReminderCommand(Guid ReminderLogId)
    : IRequest<ApiResponseBase>;
