using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.ConfirmMedicationReminder;

public sealed record ConfirmMedicationReminderCommand(Guid ReminderLogId) : IRequest<ApiResponseBase>;
