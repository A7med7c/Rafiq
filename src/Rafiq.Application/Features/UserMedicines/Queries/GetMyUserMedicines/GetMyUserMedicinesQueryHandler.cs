using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Queries.GetMyUserMedicines;

public sealed class GetMyUserMedicinesQueryHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository)
    : IRequestHandler<GetMyUserMedicinesQuery, ApiResponse<List<UserMedicineResponseDto>>>
{
    public async Task<ApiResponse<List<UserMedicineResponseDto>>> Handle(
        GetMyUserMedicinesQuery request,
        CancellationToken cancellationToken)
    {
        var profileId = request.ProfileId;

        if (profileId == Guid.Empty)
        {
            var currentUserId = currentUserService.UserId
                ?? throw new UnauthorizedException("Authentication is required.");

            profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
                ?? throw new NotFoundException("PatientProfile", currentUserId);
        }

        await authorizationService.EnsureCanReadAsync(profileId, cancellationToken);

        var userMedicines = await userMedicineRepository
            .GetAllByProfileIdAsync(profileId, cancellationToken);

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
