using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;

namespace Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminders;

public sealed record GetMedicineRemindersQuery(Guid UserMedicineId) : IRequest<ApiResponse<List<MedicineReminderResponseDto>>>;
