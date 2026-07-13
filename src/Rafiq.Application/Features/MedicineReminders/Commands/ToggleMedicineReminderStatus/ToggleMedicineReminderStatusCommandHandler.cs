using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicineReminders.Commands.ToggleMedicineReminderStatus;

public sealed class ToggleMedicineReminderStatusCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository,
    IMedicationReminderLogRepository logRepository,
    IMedicationReminderScheduler scheduler,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ToggleMedicineReminderStatusCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        ToggleMedicineReminderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var reminder = await medicineReminderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MedicineReminder), request.Id);

        var userMedicine = await userMedicineRepository.GetByIdAsync(reminder.UserMedicineId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserMedicine), reminder.UserMedicineId);

        await authorizationService.EnsureCanWriteAsync(userMedicine.UserHealthProfileId, cancellationToken);

        var wasEnabled = reminder.IsEnabled;

        reminder.ToggleStatus();

        // Only a disable (enabled -> disabled) needs to stop today's notifications; enabling
        // a reminder is out of scope here and creates no new schedule on its own.
        if (wasEnabled && !reminder.IsEnabled)
        {
            var today = dateTimeProvider.Today;
            // Guid.Empty is intentional: we want all Pending logs for this occurrence, not a subset.
            var pendingLogs = await logRepository.GetPendingOtherLogsAsync(
                reminder.Id, today, Guid.Empty, cancellationToken);

            foreach (var pendingLog in pendingLogs)
            {
                if (pendingLog.NextJobId is not null)
                    scheduler.CancelJob(pendingLog.NextJobId);

                pendingLog.Cancel();
                logRepository.Update(pendingLog);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var statusMessage = reminder.IsEnabled ? "enabled" : "disabled";

        return ApiResponse<bool>.SuccessResponse(
            reminder.IsEnabled,
            $"Medicine reminder {statusMessage} successfully.");
    }
}
