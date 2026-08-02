using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;

public sealed class AddUserMedicineCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<AddUserMedicineCommand, ApiResponse<UserMedicineResponseDto>>
{
    public async Task<ApiResponse<UserMedicineResponseDto>> Handle(
        AddUserMedicineCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.ProfileId, cancellationToken);

        var exists = await userMedicineRepository.ExistsByNameAsync(
            request.ProfileId, request.MedicineName, request.Dosage, null, cancellationToken);

        if (exists)
            throw new ValidationException(
                "This medication already exists in your medication list.");

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

        await userMedicineRepository.AddAsync(userMedicine, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.ProfileId, cancellationToken);

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
            "Medicine added to your list successfully.");
    }
}
