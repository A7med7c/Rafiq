using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicationReminderEngine.DTOs;

namespace Rafiq.Application.Features.MedicationReminderEngine.Queries.GetTodaysMedicationReminders;

public sealed record GetTodaysMedicationRemindersQuery(Guid ProfileId)
    : IRequest<ApiResponse<List<MedicationReminderLogDto>>>;
