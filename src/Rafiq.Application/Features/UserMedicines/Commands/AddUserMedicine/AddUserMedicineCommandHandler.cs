using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Application.Features.PatientProfiles.Commands.Allergies.CreateAllergy;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;

public sealed class AddUserMedicineCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IAllergyRepository allergyRepository,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache,
    IFileStorageService fileStorageService,
    IDuplicateDocumentDetector duplicateDetector,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddUserMedicineCommand, ApiResponse<UserMedicineResponseDto>>
{
    public async Task<ApiResponse<UserMedicineResponseDto>> Handle(
        AddUserMedicineCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.ProfileId, cancellationToken);

        var exists = await userMedicineRepository.ExistsByNameAsync(
            request.ProfileId, request.MedicineName, request.Dosage, null, cancellationToken);

        var userAllergies = await allergyRepository.GetAllByProfileIdAsync(request.ProfileId, cancellationToken);
        var conflictingAllergies = AllergyConflictChecker.GetConflictingAllergies(request.MedicineName, userAllergies.Select(a => a.Name));

        if (exists)
        {
            var conflictMessage = "This medication already exists in your medication list.";
            if (conflictingAllergies.Count > 0)
            {
                conflictMessage = $"This medication already exists in your medication list. خلي بالك: المريض يعاني من حساسية تجاه {string.Join("، ", conflictingAllergies)} والتي قد تتعارض مع هذا الدواء كمرجع أولي، مع ملاحظة إن القرار الطبي النهائي يعتمد على تقييم الطبيب أو الصيدلي.";
            }
            throw new ConflictException(conflictMessage);
        }

        string? fileHash = null;

        if (!string.IsNullOrEmpty(request.ImagePath))
        {
            var currentUserId = currentUserService.UserId;
            if (currentUserId.HasValue)
            {
                var imageBytes = await fileStorageService.GetFileBytesAsync(request.ImagePath, cancellationToken);
                
                var duplicateCheck = await duplicateDetector.ComputeHashAndCheckAsync(
                    imageBytes,
                    request.ImagePath,
                    request.ProfileId,
                    currentUserId.Value,
                    cancellationToken);

                if (duplicateCheck.IsDuplicate)
                {
                    if (duplicateCheck.IsSameProfile)
                    {
                        throw new DocumentValidationException("DUPLICATE_DOCUMENT", "This exact document has already been uploaded to this profile.");
                    }
                    
                    if (!request.BypassFamilyDuplicateCheck)
                    {
                        return ApiResponse<UserMedicineResponseDto>.FailureResponse(
                            "This document already exists in another family member's profile.",
                            errorCode: "DuplicateInFamily",
                            errorData: new
                            {
                                existingProfileId = duplicateCheck.ExistingProfileId,
                                existingProfileName = duplicateCheck.ExistingProfileName
                            });
                    }
                }

                fileHash = duplicateCheck.FileHash;
            }
        }

        var userMedicine = new UserMedicine(
            userHealthProfileId: request.ProfileId,
            medicineName: request.MedicineName,
            dosage: request.Dosage,
            frequency: request.Frequency,
            duration: request.Duration,
            notes: request.Notes,
            imagePath: request.ImagePath,
            source: request.Source
        );

        if (fileHash != null)
        {
            userMedicine.GetType().GetProperty(nameof(UserMedicine.FileHash))?.SetValue(userMedicine, fileHash);
        }

        await userMedicineRepository.AddAsync(userMedicine, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.ProfileId, cancellationToken);

        var message = "Medicine added to your list successfully.";
        if (conflictingAllergies.Count > 0)
        {
            message = $"Medicine added to your list successfully. خلي بالك: المريض يعاني من حساسية تجاه {string.Join("، ", conflictingAllergies)} والتي قد تتعارض مع هذا الدواء كمرجع أولي، مع ملاحظة إن القرار الطبي النهائي يعتمد على تقييم الطبيب أو الصيدلي.";
        }

        var dto = new UserMedicineResponseDto
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
        };

        return ApiResponse<UserMedicineResponseDto>.SuccessResponse(
            dto,
            message);
    }
}
