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

        // Cancel every other Pending attempt in the same dose occurrence.
        //
        // "Same occurrence" is (MedicineReminderId, ScheduledDate) — guaranteed by the
        // unique filtered index to map to exactly one set of up to three attempt logs.
        //
        // This intentionally covers BOTH earlier and later stages: confirming Stage 3
        // cancels Stage 1 and Stage 2 if they are still Pending, preventing their
        // Hangfire jobs from firing and sending spurious notifications for a dose
        // the user has already marked as taken.
        //
        // Sent logs are left untouched: their jobs have already executed, their
        // notifications have been delivered, and their historical status is preserved.
        var pendingOthers = await logRepository.GetPendingOtherLogsAsync(
            log.MedicineReminderId,
            log.ScheduledDate,
            log.Id,
            cancellationToken);

        foreach (var pending in pendingOthers)
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
