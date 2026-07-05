using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Queries.GetMyUserMedicines;

public sealed class GetMyUserMedicinesQueryHandler(
    ICurrentUserService currentUserService,
    IUserMedicineRepository userMedicineRepository)
    : IRequestHandler<GetMyUserMedicinesQuery, ApiResponse<List<UserMedicineResponseDto>>>
{
    public async Task<ApiResponse<List<UserMedicineResponseDto>>> Handle(
        GetMyUserMedicinesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var userMedicines = await userMedicineRepository
            .GetAllByUserIdAsync(userId, cancellationToken);

        var dtos = userMedicines.Select(userMedicine => new UserMedicineResponseDto
        {
            Id = userMedicine.Id,
            MedicineName = userMedicine.MedicineName,
            Dosage = userMedicine.Dosage,
            Frequency = userMedicine.Frequency,
            Duration = userMedicine.Duration,
            Notes = userMedicine.Notes,
            ImagePath = userMedicine.ImagePath,
            Source = userMedicine.Source.ToString(),
            CreatedAt = userMedicine.CreatedAt,
            UpdatedAt = userMedicine.UpdatedAt
        }).ToList();

        return ApiResponse<List<UserMedicineResponseDto>>.SuccessResponse(
            dtos,
            "Medicines retrieved successfully.");
    }
}
