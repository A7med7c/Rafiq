using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.MedicineReminders.Commands.ToggleMedicineReminderStatus;

public sealed record ToggleMedicineReminderStatusCommand(Guid Id) : IRequest<ApiResponse<bool>>;
