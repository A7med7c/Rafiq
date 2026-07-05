using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;

public sealed record AddUserMedicineCommand(
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string? Notes,
    string? ImagePath,
    MedicineSource Source)
    : IRequest<ApiResponse<UserMedicineResponseDto>>;
