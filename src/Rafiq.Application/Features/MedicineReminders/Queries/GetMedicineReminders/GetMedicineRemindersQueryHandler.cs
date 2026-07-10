using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminders;

public sealed class GetMedicineRemindersQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository)
    : IRequestHandler<GetMedicineRemindersQuery, ApiResponse<List<MedicineReminderResponseDto>>>
{
    public async Task<ApiResponse<List<MedicineReminderResponseDto>>> Handle(
        GetMedicineRemindersQuery request,
        CancellationToken cancellationToken)
    {
        var userMedicine = await userMedicineRepository.GetByIdAsync(request.UserMedicineId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserMedicine), request.UserMedicineId);

        await authorizationService.EnsureCanReadAsync(userMedicine.UserHealthProfileId, cancellationToken);

        var reminders = await medicineReminderRepository.GetByUserMedicineIdAsync(request.UserMedicineId, cancellationToken);

        var dtos = reminders.Select(r => new MedicineReminderResponseDto
        {
            Id = r.Id,
            UserMedicineId = r.UserMedicineId,
            ReminderTime = r.ReminderTime,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            RepeatType = r.RepeatType.ToString(),
            IsEnabled = r.IsEnabled,
            LastTriggeredAt = r.LastTriggeredAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return ApiResponse<List<MedicineReminderResponseDto>>.SuccessResponse(dtos);
    }
}
