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
    IHealthProfileAuthorizationService authorizationService,
    IPrescriptionRepository prescriptionRepository,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddFromPrescriptionCommand, ApiResponse<AddFromPrescriptionResponseDto>>
{
    public async Task<ApiResponse<AddFromPrescriptionResponseDto>> Handle(
        AddFromPrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load all requested PrescriptionMedicines with their parent Prescriptions
        var prescriptionMedicines = await prescriptionRepository.GetMedicinesByIdsAsync(
            request.PrescriptionMedicineIds,
            cancellationToken);

        // 2. Verify every requested medicine was found
        if (prescriptionMedicines.Count != request.PrescriptionMedicineIds.Count)
        {
            var foundIds = prescriptionMedicines.Select(m => m.Id).ToHashSet();
            var missingId = request.PrescriptionMedicineIds.First(id => !foundIds.Contains(id));
            throw new NotFoundException(nameof(PrescriptionMedicine), missingId);
        }

        // 3. Collect distinct source UserHealthProfileIds from actual parent Prescriptions
        var distinctSourceProfileIds = prescriptionMedicines
            .Select(m => m.Prescription.UserHealthProfileId)
            .Distinct()
            .ToList();

        // 4. EnsureCanReadAsync once per distinct source profile
        foreach (var sourceProfileId in distinctSourceProfileIds)
        {
            await authorizationService.EnsureCanReadAsync(sourceProfileId, cancellationToken);
        }

        // 5. EnsureCanWriteAsync once for the destination profile
        await authorizationService.EnsureCanWriteAsync(request.ProfileId, cancellationToken);

        // 6. Fetch existing UserMedicines for the destination profile to avoid duplicates
        var existingUserMedicines = await userMedicineRepository.GetAllByProfileIdAsync(request.ProfileId, cancellationToken);
        var existingNames = new HashSet<string>(
            existingUserMedicines.Select(m => m.MedicineName.Trim()),
            StringComparer.OrdinalIgnoreCase);

        int addedCount = 0;
        int skippedCount = 0;

        // 7. Process each medicine
        foreach (var pm in prescriptionMedicines)
        {
            var medName = pm.MedicineName.Trim();

            if (existingNames.Contains(medName))
            {
                skippedCount++;
                continue;
            }

            var newUserMedicine = new UserMedicine(
                userHealthProfileId: request.ProfileId,
                medicineName: pm.MedicineName,
                dosage: pm.Dosage,
                frequency: pm.Frequency,
                duration: pm.Duration,
                notes: pm.Notes,
                imagePath: null,
                source: MedicineSource.Prescription
            );

            await userMedicineRepository.AddAsync(newUserMedicine, cancellationToken);
            existingNames.Add(medName);
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
