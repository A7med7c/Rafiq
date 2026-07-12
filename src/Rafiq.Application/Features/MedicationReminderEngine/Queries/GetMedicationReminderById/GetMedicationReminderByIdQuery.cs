using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetMedicationReminderById;

public sealed record GetMedicationReminderByIdQuery(Guid Id)
    : IRequest<ApiResponse<MedicationReminderLogDto>>;
