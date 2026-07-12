using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicineReminders.Commands.DeleteMedicineReminder;

public sealed class DeleteMedicineReminderCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository,
    IMedicationReminderLogRepository logRepository,
    IMedicationReminderScheduler scheduler,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMedicineReminderCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteMedicineReminderCommand request,
        CancellationToken cancellationToken)
    {
        var reminder = await medicineReminderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineReminder), request.Id);

        var userMedicine = await userMedicineRepository.GetByIdAsync(reminder.UserMedicineId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserMedicine), reminder.UserMedicineId);

        await authorizationService.EnsureCanWriteAsync(userMedicine.UserHealthProfileId, cancellationToken);

        // A deleted reminder must never notify again: cancel every not-yet-fired Hangfire job
        // for today and mark those logs Cancelled before the reminder itself is soft-deleted.
        var today = dateTimeProvider.Today;
        var pendingLogs = await logRepository.GetPendingSubsequentLogsAsync(
            reminder.Id, today, afterReminderNumber: 0, cancellationToken);

        foreach (var pendingLog in pendingLogs)
        {
            if (pendingLog.NextJobId is not null)
                scheduler.CancelJob(pendingLog.NextJobId);

            pendingLog.Cancel();
            logRepository.Update(pendingLog);
        }

        medicineReminderRepository.Delete(reminder);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(
            true,
            "Medicine reminder deleted successfully.");
    }
}
