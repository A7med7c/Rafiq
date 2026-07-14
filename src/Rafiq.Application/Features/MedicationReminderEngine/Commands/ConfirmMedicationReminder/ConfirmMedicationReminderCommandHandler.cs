using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.ConfirmMedicationReminder;

public sealed class ConfirmMedicationReminderCommandHandler(
    IMedicationReminderLogRepository logRepository,
    IHealthProfileAuthorizationService authorizationService,
    IMedicationReminderScheduler scheduler,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmMedicationReminderCommand, ApiResponseBase>
{
    public async Task<ApiResponseBase> Handle(
        ConfirmMedicationReminderCommand request,
        CancellationToken cancellationToken)
    {
        var log = await logRepository.GetByIdAsync(request.ReminderLogId, cancellationToken)
            ?? throw new NotFoundException("MedicationReminderLog", request.ReminderLogId);

        await authorizationService.EnsureCanWriteAsync(log.UserHealthProfileId, cancellationToken);

        // Idempotent guard: a reminder can only ever be confirmed once.  A stale
        // notification/dialog, a duplicate click, or a second browser tab racing this
        // same request must not throw or mutate the log again — just report the final
        // state that already exists so the caller can treat it as a graceful no-op.
        if (log.Status == MedicationReminderStatus.Confirmed)
            return ApiResponseBase.FailureResponse("Medication has already been marked as taken.");

        if (log.Status == MedicationReminderStatus.Cancelled)
            return ApiResponseBase.FailureResponse("This reminder is no longer active and cannot be confirmed.");

        // Cancel this attempt's own Hangfire job in case it was confirmed before it fired.
        if (log.NextJobId is not null)
            scheduler.CancelJob(log.NextJobId);

        log.MarkAsConfirmed();
        logRepository.Update(log);

        // Mark any pending subsequent logs for the same schedule+day as Cancelled
        var pendingNext = await logRepository.GetPendingOtherLogsAsync(
         log.MedicineReminderId,
         log.ScheduledDate,
         log.Id,
         cancellationToken);
        foreach (var pending in pendingNext)
        {
            if (pending.NextJobId is not null)
                scheduler.CancelJob(pending.NextJobId);

            pending.Cancel();
            logRepository.Update(pending);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponseBase.SuccessResponse("Medication confirmed successfully.");
    }
}
