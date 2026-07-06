using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminderById;

public sealed class GetMedicineReminderByIdQueryHandler(
    ICurrentUserService currentUserService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository)
    : IRequestHandler<GetMedicineReminderByIdQuery, ApiResponse<MedicineReminderResponseDto>>
{
    public async Task<ApiResponse<MedicineReminderResponseDto>> Handle(
        GetMedicineReminderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var reminder = await medicineReminderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineReminder), request.Id);

        var userMedicine = await userMedicineRepository.GetByIdAsync(reminder.UserMedicineId, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserMedicine), reminder.UserMedicineId);

        // Authorization is handled by repository

        var dto = new MedicineReminderResponseDto
        {
            Id = reminder.Id,
            UserMedicineId = reminder.UserMedicineId,
            ReminderTime = reminder.ReminderTime,
            StartDate = reminder.StartDate,
            EndDate = reminder.EndDate,
            RepeatType = reminder.RepeatType.ToString(),
            IsEnabled = reminder.IsEnabled,
            LastTriggeredAt = reminder.LastTriggeredAt,
            CreatedAt = reminder.CreatedAt,
            UpdatedAt = reminder.UpdatedAt
        };

        return ApiResponse<MedicineReminderResponseDto>.SuccessResponse(dto);
    }
}
