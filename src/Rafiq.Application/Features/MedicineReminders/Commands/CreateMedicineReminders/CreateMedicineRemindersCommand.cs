using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;

public sealed record CreateMedicineRemindersCommand(
    Guid UserMedicineId,
    DateOnly StartDate,
    DateOnly EndDate,
    RepeatType RepeatType,
    List<string> Times)
    : IRequest<ApiResponse<List<MedicineReminderResponseDto>>>;
