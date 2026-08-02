using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;

public sealed class DeleteUserMedicineCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository,
    IMedicationReminderLogRepository logRepository,
    IMedicationReminderScheduler scheduler,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<DeleteUserMedicineCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteUserMedicineCommand request,
        CancellationToken cancellationToken)
    {
        var userMedicine = await userMedicineRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.UserMedicine), request.Id);

        await authorizationService.EnsureCanWriteAsync(userMedicine.UserHealthProfileId, cancellationToken);

        // Cancel every pending reminder notification for this medicine and soft-delete
        // all its reminder schedules so the daily sweep never re-schedules them.
        // Confirmed and sent logs are intentionally preserved for history.
        var reminders = await medicineReminderRepository.GetByUserMedicineIdAsync(
            userMedicine.Id, cancellationToken);

        var today = dateTimeProvider.Today;

        foreach (var reminder in reminders)
        {
            // Guid.Empty: cancel ALL pending logs for this occurrence, not a subset.
            var pendingLogs = await logRepository.GetPendingOtherLogsAsync(
                reminder.Id, today, Guid.Empty, cancellationToken);

            foreach (var pendingLog in pendingLogs)
            {
                if (pendingLog.NextJobId is not null)
                    scheduler.CancelJob(pendingLog.NextJobId);

                pendingLog.Cancel();
                logRepository.Update(pendingLog);
            }

            medicineReminderRepository.Delete(reminder);
        }

        var profileId = userMedicine.UserHealthProfileId;
        userMedicineRepository.Delete(userMedicine);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(profileId, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Medicine deleted successfully.");
    }
}
