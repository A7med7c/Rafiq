using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetMedicationReminderHistory;

public sealed class GetMedicationReminderHistoryQueryHandler(
    IMedicationReminderLogRepository logRepository,
    IHealthProfileAuthorizationService authorizationService)
    : IRequestHandler<GetMedicationReminderHistoryQuery, ApiResponse<List<MedicationReminderLogDto>>>
{
    public async Task<ApiResponse<List<MedicationReminderLogDto>>> Handle(
        GetMedicationReminderHistoryQuery request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var logs = await logRepository.GetByDateAndProfileIdAsync(
            request.ProfileId, request.Date, cancellationToken);

        var dtos = logs
            .GroupBy(l => (l.MedicineReminderId, l.ScheduledDate))
            .SelectMany(group =>
            {
                var ordered = group.OrderBy(l => l.ReminderNumber).ToList();
                // For history, no log is actionable — the date has passed.
                var actionableId = request.Date >= DateOnly.FromDateTime(DateTime.UtcNow.Date)
                    ? DetermineActionableLogId(ordered)
                    : (Guid?)null;
                return ordered.Select(l => l.ToDto(isActionable: l.Id == actionableId));
            })
            .OrderBy(d => d.ReminderTime)
            .ThenBy(d => d.ReminderNumber)
            .ToList();

        return ApiResponse<List<MedicationReminderLogDto>>.SuccessResponse(dtos);
    }

    private static Guid? DetermineActionableLogId(List<MedicationReminderLog> logs)
    {
        if (logs.Any(l => l.Status is MedicationReminderStatus.Confirmed
                              or MedicationReminderStatus.Skipped))
            return null;

        var sent = logs.LastOrDefault(l => l.Status == MedicationReminderStatus.Sent);
        if (sent is not null) return sent.Id;

        var overdue = logs.FirstOrDefault(l => l.Status == MedicationReminderStatus.Overdue);
        if (overdue is not null) return overdue.Id;

        var pending = logs.FirstOrDefault(l => l.Status == MedicationReminderStatus.Pending);
        return pending?.Id;
    }
}
