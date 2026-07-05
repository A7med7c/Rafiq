using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddFromPrescription;

public sealed class AddFromPrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IPrescriptionRepository prescriptionRepository,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddFromPrescriptionCommand, ApiResponse<AddFromPrescriptionResponseDto>>
{
    public async Task<ApiResponse<AddFromPrescriptionResponseDto>> Handle(
        AddFromPrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // 1. Fetch the requested PrescriptionMedicines that belong to the current user
        var prescriptionMedicines = await prescriptionRepository.GetMedicinesByIdsAsync(
            request.PrescriptionMedicineIds, 
            userId, 
            cancellationToken);

        if (prescriptionMedicines.Count == 0)
        {
            return ApiResponse<AddFromPrescriptionResponseDto>.SuccessResponse(
                new AddFromPrescriptionResponseDto { AddedCount = 0, SkippedCount = 0 },
                "No valid medicines found to add.");
        }

        // 2. Fetch all existing UserMedicines for the user to check for duplicates
        var existingUserMedicines = await userMedicineRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var existingNames = new HashSet<string>(
            existingUserMedicines.Select(m => m.MedicineName.Trim()), 
            StringComparer.OrdinalIgnoreCase);

        int addedCount = 0;
        int skippedCount = 0;

        // 3. Process each requested medicine
        foreach (var pm in prescriptionMedicines)
        {
            var medName = pm.MedicineName.Trim();

            if (existingNames.Contains(medName))
            {
                skippedCount++;
                continue;
            }

            var newUserMedicine = new UserMedicine(
                userId: userId,
                medicineName: pm.MedicineName,
                dosage: pm.Dosage,
                frequency: pm.Frequency,
                duration: pm.Duration,
                notes: pm.Notes,
                imagePath: null,
                source: MedicineSource.Prescription
            );

            await userMedicineRepository.AddAsync(newUserMedicine, cancellationToken);
            existingNames.Add(medName); // Prevent duplicate in the same batch
            addedCount++;
        }

        if (addedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dto = new AddFromPrescriptionResponseDto
        {
            AddedCount = addedCount,
            SkippedCount = skippedCount
        };

        return ApiResponse<AddFromPrescriptionResponseDto>.SuccessResponse(
            dto,
            $"Added {addedCount} medicines. Skipped {skippedCount} duplicates.");
    }
}
