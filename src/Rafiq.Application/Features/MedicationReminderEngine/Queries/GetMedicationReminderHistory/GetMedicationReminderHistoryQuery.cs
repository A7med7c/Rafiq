using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetMedicationReminderHistory;

public sealed record GetMedicationReminderHistoryQuery(Guid ProfileId, DateOnly Date)
    : IRequest<ApiResponse<List<MedicationReminderLogDto>>>;
