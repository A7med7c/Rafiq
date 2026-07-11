using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicineReminders.Commands.CreateMedicineReminders;

public sealed class CreateMedicineRemindersCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IMedicineReminderRepository medicineReminderRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicineRemindersCommand, ApiResponse<List<MedicineReminderResponseDto>>>
{
    public async Task<ApiResponse<List<MedicineReminderResponseDto>>> Handle(
        CreateMedicineRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var userMedicine = await userMedicineRepository.GetByIdAsync(request.UserMedicineId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserMedicine), request.UserMedicineId);

        await authorizationService.EnsureCanWriteAsync(userMedicine.UserHealthProfileId, cancellationToken);

        var duplicateTimes = new List<string>();
        var reminders = new List<MedicineReminder>();

        foreach (var timeString in request.Times)
        {
            if (TimeSpan.TryParse(timeString, out var timeSpan))
            {
                var exists = await medicineReminderRepository.ExistsAsync(
                    request.UserMedicineId,
                    timeSpan,
                    request.StartDate,
                    request.EndDate,
                    request.RepeatType.ToString(),
                    null,
                    cancellationToken);

                if (exists)
                {
                    duplicateTimes.Add(timeString);
                    continue;
                }

                var reminder = new MedicineReminder(
                    request.UserMedicineId,
                    timeSpan,
                    request.StartDate,
                    request.EndDate,
                    request.RepeatType);
                
                reminders.Add(reminder);
            }
            else
            {
                throw new ValidationException(new[] { $"Invalid time format: {timeString}" });
            }
        }

        if (duplicateTimes.Any())
        {
            throw new ValidationException(new[] { $"The following reminder times already exist for this medicine: {string.Join(", ", duplicateTimes)}" });
        }

        await medicineReminderRepository.AddRangeAsync(reminders, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dtos = reminders.Select(r => new MedicineReminderResponseDto
        {
            Id = r.Id,
            UserMedicineId = r.UserMedicineId,
            ReminderTime = r.ReminderTime,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            RepeatType = r.RepeatType.ToString(),
            IsEnabled = r.IsEnabled,
            LastTriggeredAt = r.LastTriggeredAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return ApiResponse<List<MedicineReminderResponseDto>>.SuccessResponse(
            dtos,
            "Medicine reminders created successfully.");
    }
}
