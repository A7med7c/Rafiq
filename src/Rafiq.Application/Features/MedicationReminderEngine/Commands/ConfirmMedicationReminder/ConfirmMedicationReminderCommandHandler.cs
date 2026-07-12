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

        if (log.Status == MedicationReminderStatus.Confirmed)
            throw new BadRequestException("Medication has already been confirmed.");

        if (log.Status == MedicationReminderStatus.Cancelled)
            throw new BadRequestException("This reminder has been cancelled and cannot be confirmed.");

        // Cancel this stage's own job, in case it was confirmed before it fired
        if (log.NextJobId is not null)
            scheduler.CancelJob(log.NextJobId);

        log.MarkAsConfirmed();
        logRepository.Update(log);

        // Every remaining stage (all three are scheduled up front) must be cancelled: both
        // its Hangfire job and its log, so nothing further can send or be re-triggered.
        var pendingNext = await logRepository.GetPendingSubsequentLogsAsync(
            log.MedicineReminderId,
            log.ScheduledDate,
            log.ReminderNumber,
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
