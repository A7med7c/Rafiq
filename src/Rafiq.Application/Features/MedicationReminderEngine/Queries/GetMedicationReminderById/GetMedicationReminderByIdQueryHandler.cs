using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetMedicationReminderById;

public sealed class GetMedicationReminderByIdQueryHandler(
    IMedicationReminderLogRepository logRepository,
    IHealthProfileAuthorizationService authorizationService)
    : IRequestHandler<GetMedicationReminderByIdQuery, ApiResponse<MedicationReminderLogDto>>
{
    public async Task<ApiResponse<MedicationReminderLogDto>> Handle(
        GetMedicationReminderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var log = await logRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("MedicationReminderLog", request.Id);

        await authorizationService.EnsureCanReadAsync(log.UserHealthProfileId, cancellationToken);

        return ApiResponse<MedicationReminderLogDto>.SuccessResponse(log.ToDto());
    }
}
