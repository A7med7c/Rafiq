using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetTodaysMedicationReminders;

public sealed class GetTodaysMedicationRemindersQueryHandler(
    IMedicationReminderLogRepository logRepository,
    IHealthProfileAuthorizationService authorizationService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetTodaysMedicationRemindersQuery, ApiResponse<List<MedicationReminderLogDto>>>
{
    public async Task<ApiResponse<List<MedicationReminderLogDto>>> Handle(
        GetTodaysMedicationRemindersQuery request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var today = dateTimeProvider.Today;
        var logs = await logRepository.GetTodayByProfileIdAsync(request.ProfileId, today, cancellationToken);

        // Group by (MedicineReminderId, ScheduledDate) — which is guaranteed by the unique
        // filtered index to represent exactly one dose occurrence — then determine which single
        // log within each group is currently actionable.  The frontend receives this flag
        // directly so it never needs to infer it from ReminderNumber or current wall-clock time.
        var dtos = logs
            .GroupBy(l => (l.MedicineReminderId, l.ScheduledDate))
            .SelectMany(group =>
            {
                var ordered = group.OrderBy(l => l.ReminderNumber).ToList();
                var actionableId = DetermineActionableLogId(ordered);
                return ordered.Select(l => l.ToDto(isActionable: l.Id == actionableId));
            })
            .OrderBy(d => d.ReminderTime)
            .ThenBy(d => d.ReminderNumber)
            .ToList();

        return ApiResponse<List<MedicationReminderLogDto>>.SuccessResponse(dtos);
    }

    /// <summary>
    /// Identifies which single log in an occurrence the user should confirm right now.
    /// Returns null when the occurrence is already complete (Confirmed or all Cancelled).
    ///
    /// Priority order:
    ///   1. If any log is Confirmed → occurrence is done, nothing is actionable.
    ///   2. Most-recently-sent log (highest ReminderNumber with Status = Sent) — a notification
    ///      has been pushed to the user; this is the attempt currently awaiting an answer.
    ///   3. First Overdue log — time has passed, no notification was sent, but the user can
    ///      still confirm the dose late.
    ///   4. First Pending log — no notification has fired yet; early confirmation is allowed
    ///      so the user can mark the dose taken before the first attempt fires.
    /// </summary>
    private static Guid? DetermineActionableLogId(List<MedicationReminderLog> logs)
    {
        // Occurrence is done — no log should be actionable.
        if (logs.Any(l => l.Status is MedicationReminderStatus.Confirmed
                              or MedicationReminderStatus.Skipped))
            return null;

        // Snoozed logs are intentionally waiting — not actionable until the snooze fires
        // and transitions them back to Sent.
        var sent = logs.LastOrDefault(l => l.Status == MedicationReminderStatus.Sent);
        if (sent is not null) return sent.Id;

        var overdue = logs.FirstOrDefault(l => l.Status == MedicationReminderStatus.Overdue);
        if (overdue is not null) return overdue.Id;

        var pending = logs.FirstOrDefault(l => l.Status == MedicationReminderStatus.Pending);
        return pending?.Id;
    }
}
