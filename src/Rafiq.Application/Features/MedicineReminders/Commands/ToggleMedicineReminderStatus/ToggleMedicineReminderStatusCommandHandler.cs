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

        reminder.ToggleStatus();

        medicineReminderRepository.Update(reminder);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var statusMessage = reminder.IsEnabled ? "enabled" : "disabled";

        return ApiResponse<bool>.SuccessResponse(
            reminder.IsEnabled,
            $"Medicine reminder {statusMessage} successfully.");
    }
}
