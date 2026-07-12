using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;
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

        // Must match the scheduler's notion of "today", otherwise the two disagree under a non-UTC zone.
        var today = dateTimeProvider.Today;
        var logs = await logRepository.GetTodayByProfileIdAsync(request.ProfileId, today, cancellationToken);

        var dtos = logs.Select(l => l.ToDto()).ToList();

        return ApiResponse<List<MedicationReminderLogDto>>.SuccessResponse(dtos);
    }
}
