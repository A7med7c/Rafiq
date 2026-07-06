using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.MedicineReminders.DTOs;

namespace Rafiq.Application.Features.MedicineReminders.Queries.GetMedicineReminderById;

public sealed record GetMedicineReminderByIdQuery(Guid Id) : IRequest<ApiResponse<MedicineReminderResponseDto>>;
