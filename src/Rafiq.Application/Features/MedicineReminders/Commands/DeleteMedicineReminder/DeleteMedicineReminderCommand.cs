using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.MedicineReminders.Commands.DeleteMedicineReminder;

public sealed record DeleteMedicineReminderCommand(Guid Id) : IRequest<ApiResponse<bool>>;
