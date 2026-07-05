using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;

namespace Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;

public sealed record UpdateUserMedicineCommand(
    Guid Id,
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string? Notes)
    : IRequest<ApiResponse<UserMedicineResponseDto>>;
