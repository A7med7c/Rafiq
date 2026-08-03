using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.SkipMedicationReminder;

public sealed class SkipMedicationReminderCommandHandler(
    IMedicationReminderLogRepository logRepository,
    IHealthProfileAuthorizationService authorizationService,
    IMedicationReminderScheduler scheduler,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SkipMedicationReminderCommand, ApiResponseBase>
{
    public async Task<ApiResponseBase> Handle(
        SkipMedicationReminderCommand request,
        CancellationToken cancellationToken)
    {
        var log = await logRepository.GetByIdAsync(request.ReminderLogId, cancellationToken)
            ?? throw new NotFoundException("MedicationReminderLog", request.ReminderLogId);

        await authorizationService.EnsureCanWriteAsync(log.UserHealthProfileId, cancellationToken);

        if (log.Status == MedicationReminderStatus.Skipped)
            return ApiResponseBase.FailureResponse("This dose has already been skipped.");

        if (log.IsCompleted)
            return ApiResponseBase.FailureResponse("This reminder is no longer active.");

        // Cancel any pending Hangfire job for this specific log.
        if (log.NextJobId is not null)
            scheduler.CancelJob(log.NextJobId);

        log.MarkAsSkipped();
        logRepository.Update(log);

        // Cancel sibling Pending and Snoozed logs for the same occurrence.
        var siblings = await logRepository.GetActiveOtherLogsForSkipAsync(
            log.MedicineReminderId,
            log.ScheduledDate,
            log.Id,
            cancellationToken);

        foreach (var sibling in siblings)
        {
            if (sibling.NextJobId is not null)
                scheduler.CancelJob(sibling.NextJobId);

            sibling.Cancel();
            logRepository.Update(sibling);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponseBase.SuccessResponse("Dose skipped.");
    }
}
