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
    IUnitOfWork unitOfWork, IMedicationReminderLogRepository logRepository,
    IDateTimeProvider dateTimeProvider, IAppointmentReminderScheduler schedulingService)
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

        if (request.StartDate != reminder.StartDate && request.StartDate < dateTimeProvider.Today)
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

        var today = dateTimeProvider.Today;
        var staleLogs = await logRepository.GetPendingAndOverdueLogsAsync(reminder.Id, today, cancellationToken);

        foreach (var staleLog in staleLogs)
        {
            if (staleLog.NextJobId is not null)
                schedulingService.CancelJob(staleLog.NextJobId);

            staleLog.Cancel();
            logRepository.Update(staleLog);
        }

        // Recalculate the reminder's own time/dates/repeat type.
        reminder.UpdateDetails(request.ReminderTime, request.StartDate, request.EndDate, request.RepeatType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Recreate today's schedule from the new details: three fresh logs, three fresh jobs,
        // computed from the recalculated reminder time.
        await medicationSchedulingService.ScheduleTodayIfApplicableAsync(reminder, cancellationToken);

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
