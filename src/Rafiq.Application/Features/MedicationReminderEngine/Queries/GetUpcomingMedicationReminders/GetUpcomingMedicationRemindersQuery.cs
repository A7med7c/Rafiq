using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Common.DTOs;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetUpcomingMedicationReminders;

public sealed record GetUpcomingMedicationRemindersQuery(Guid ProfileId)
    : IRequest<ApiResponse<List<UpcomingReminderDto>>>;
