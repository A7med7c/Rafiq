using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Common.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetUpcomingMedicationReminders;

/// <summary>
/// Returns today's upcoming medication reminder occurrences mapped to the shared
/// <see cref="UpcomingReminderDto"/> offline-sync contract.
///
/// This handler is COMPLETELY READ-ONLY:
///   ✓ Does NOT insert MedicationReminderLog records
///   ✓ Does NOT schedule Hangfire jobs
///   ✓ Does NOT publish SignalR events
///   ✓ Does NOT update reminder statuses
///   ✓ Does NOT modify the database in any way
///
/// It delegates to <see cref="IMedicationSchedulingService.GetUpcomingStagesAsync"/>,
/// which performs the same pure occurrence calculation as the Hangfire scheduling path
/// but without any persistence.
/// </summary>
public sealed class GetUpcomingMedicationRemindersQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IMedicineReminderRepository reminderRepository,
    IMedicationSchedulingService schedulingService)
    : IRequestHandler<GetUpcomingMedicationRemindersQuery, ApiResponse<List<UpcomingReminderDto>>>
{
    public async Task<ApiResponse<List<UpcomingReminderDto>>> Handle(
        GetUpcomingMedicationRemindersQuery request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        // Load all active reminders for this profile (includes UserMedicine navigation).
        var reminders = await reminderRepository
            .GetAllWithMedicineByProfileIdAsync(request.ProfileId, cancellationToken);

        var dtos = new List<UpcomingReminderDto>();

        foreach (var reminder in reminders)
        {
            // Pure read — no DB writes, no Hangfire, no SignalR.
            var stages = await schedulingService.GetUpcomingStagesAsync(reminder, cancellationToken);

            foreach (var (log, scheduledUtc, _) in stages)
            {
                var medicineName = reminder.UserMedicine?.MedicineName ?? string.Empty;

                dtos.Add(new UpcomingReminderDto
                {
                    ReminderId   = reminder.Id,
                    EntityId     = reminder.UserMedicineId,
                    ReminderType = ReminderType.Medication,
                    Title        = medicineName,
                    Body         = $"Time to take {medicineName}",
                    ScheduledAt  = new DateTimeOffset(scheduledUtc, TimeSpan.Zero),
                    Status       = log.Status.ToString(),
                    UpdatedAt    = new DateTimeOffset(reminder.UpdatedAt ?? reminder.CreatedAt, TimeSpan.Zero),
                    IsDeleted    = reminder.IsDeleted,
                    Payload      = null   // No extra metadata required; top-level fields are sufficient.
                });
            }
        }

        // Sort chronologically so the mobile client gets a ready-to-use ordered list.
        dtos.Sort((a, b) => a.ScheduledAt.CompareTo(b.ScheduledAt));

        return ApiResponse<List<UpcomingReminderDto>>.SuccessResponse(
            dtos,
            "Upcoming medication reminders retrieved successfully.");
    }
}
