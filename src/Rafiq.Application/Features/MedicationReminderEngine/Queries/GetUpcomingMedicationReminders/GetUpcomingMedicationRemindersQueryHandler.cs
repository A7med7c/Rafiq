using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Common.DTOs;
using Rafiq.Domain.Enums;
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
///
/// A stage whose persisted log has already reached a terminal state (Confirmed,
/// Cancelled, Skipped, or Missed) is excluded from the result. Without this, a mobile
/// client that already cancelled its native alarms for a confirmed dose would have them
/// resurrected on its very next sync, because this handler recomputes all three escalation
/// stages purely from the reminder's configured time — it does not otherwise look at
/// persisted log status at all.
/// </summary>
public sealed class GetUpcomingMedicationRemindersQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IMedicineReminderRepository reminderRepository,
    IMedicationSchedulingService schedulingService,
    IMedicationReminderLogRepository logRepository,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetUpcomingMedicationRemindersQuery, ApiResponse<List<UpcomingReminderDto>>>
{
    private static readonly HashSet<MedicationReminderStatus> TerminalStatuses = new()
    {
        MedicationReminderStatus.Confirmed,
        MedicationReminderStatus.Cancelled,
        MedicationReminderStatus.Skipped,
        MedicationReminderStatus.Missed,
    };

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

        // Stages whose persisted log already reached a terminal state — these must not be
        // re-surfaced as "upcoming" even though CalculateUpcomingOccurrences would otherwise
        // recompute them purely from the reminder's configured time.
        var today = dateTimeProvider.Today;
        var persistedLogs = await logRepository.GetAllStatusesForDateAsync(request.ProfileId, today, cancellationToken);
        var terminalStages = persistedLogs
            .Where(l => TerminalStatuses.Contains(l.Status))
            .Select(l => (l.MedicineReminderId, l.ReminderNumber))
            .ToHashSet();

        var dtos = new List<UpcomingReminderDto>();

        foreach (var reminder in reminders)
        {
            // Pure read — no DB writes, no Hangfire, no SignalR.
            var stages = await schedulingService.GetUpcomingStagesAsync(reminder, cancellationToken);

            foreach (var (log, scheduledUtc, _) in stages)
            {
                if (terminalStages.Contains((reminder.Id, log.ReminderNumber)))
                    continue; // Already confirmed/cancelled/skipped/missed — do not resurface.

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
