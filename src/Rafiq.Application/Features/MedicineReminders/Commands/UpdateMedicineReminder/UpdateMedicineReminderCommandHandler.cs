using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicineReminders.Commands.UpdateMedicineReminder;

public sealed class UpdateMedicineReminderCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMedicineReminderCommand, ApiResponse<MedicineReminderResponseDto>>
{
    public async Task<ApiResponse<MedicineReminderResponseDto>> Handle(
        UpdateMedicineReminderCommand request,
        CancellationToken cancellationToken)
    {
        var reminder = await medicineReminderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineReminder), request.Id);

        var userMedicine = await userMedicineRepository.GetByIdAsync(reminder.UserMedicineId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserMedicine), reminder.UserMedicineId);

        await authorizationService.EnsureCanWriteAsync(userMedicine.UserHealthProfileId, cancellationToken);

        if (request.StartDate != reminder.StartDate && request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException(new[] { "StartDate cannot be before today's date when modifying it." });
        }

        var exists = await medicineReminderRepository.ExistsAsync(
            reminder.UserMedicineId,
            request.ReminderTime,
            request.StartDate,
            request.EndDate,
            request.RepeatType.ToString(),
            reminder.Id,
            cancellationToken);

        if (exists)
        {
            throw new ValidationException(new[] { "A reminder with the same details already exists." });
        }

        reminder.UpdateDetails(request.ReminderTime, request.StartDate, request.EndDate, request.RepeatType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

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

        return ApiResponse<MedicineReminderResponseDto>.SuccessResponse(
            dto,
            "Medicine reminder updated successfully.");
    }
}
