using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.Commands.UpdateMedicineReminder;

public sealed record UpdateMedicineReminderCommand(
    Guid Id,
    TimeSpan ReminderTime,
    DateOnly StartDate,
    DateOnly EndDate,
    RepeatType RepeatType)
    : IRequest<ApiResponse<MedicineReminderResponseDto>>;
