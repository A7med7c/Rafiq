using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Commands.SnoozeMedicationReminder;

public sealed class SnoozeMedicationReminderCommandHandler(
    IMedicationReminderLogRepository logRepository,
    IHealthProfileAuthorizationService authorizationService,
    IMedicationReminderScheduler scheduler,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SnoozeMedicationReminderCommand, ApiResponseBase>
{
    private const int MinSnoozeMinutes = 1;
    private const int MaxSnoozeMinutes = 120;

    public async Task<ApiResponseBase> Handle(
        SnoozeMedicationReminderCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SnoozeMinutes < MinSnoozeMinutes || request.SnoozeMinutes > MaxSnoozeMinutes)
            return ApiResponseBase.FailureResponse(
                $"Snooze interval must be between {MinSnoozeMinutes} and {MaxSnoozeMinutes} minutes.");

        var log = await logRepository.GetByIdAsync(request.ReminderLogId, cancellationToken)
            ?? throw new NotFoundException("MedicationReminderLog", request.ReminderLogId);

        await authorizationService.EnsureCanWriteAsync(log.UserHealthProfileId, cancellationToken);

        if (log.IsCompleted)
            return ApiResponseBase.FailureResponse("This reminder is no longer active.");

        if (log.Status == MedicationReminderStatus.Snoozed)
            return ApiResponseBase.FailureResponse("This reminder is already snoozed.");

        // Cancel the currently scheduled Hangfire delivery job for this attempt so
        // it does not fire again independently while we re-schedule it after the snooze.
        if (log.NextJobId is not null)
            scheduler.CancelJob(log.NextJobId);

        // Mark snoozed first so that if the Hangfire job fires before we persist the
        // new job ID, MedicationReminderJob will see Snoozed and treat it correctly.
        log.MarkAsSnoozed();
        logRepository.Update(log);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Schedule the re-delivery job. MedicationReminderJob.ExecuteAsync guards against
        // IsCompleted and will transition the log from Snoozed → Sent when it fires.
        var delay = TimeSpan.FromMinutes(request.SnoozeMinutes);
        var newJobId = scheduler.ScheduleDelayedReminderJob(log.Id, delay);

        log.SetNextJobId(newJobId);
        logRepository.Update(log);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponseBase.SuccessResponse($"Reminder snoozed for {request.SnoozeMinutes} minutes.");
    }
}
