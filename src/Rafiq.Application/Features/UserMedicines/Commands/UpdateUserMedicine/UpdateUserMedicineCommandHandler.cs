using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;

public sealed class UpdateUserMedicineCommandHandler(
    ICurrentUserService currentUserService,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserMedicineCommand, ApiResponse<UserMedicineResponseDto>>
{
    public async Task<ApiResponse<UserMedicineResponseDto>> Handle(
        UpdateUserMedicineCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var userMedicine = await userMedicineRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.UserMedicine), request.Id);

        userMedicine.MedicineName = request.MedicineName;
        userMedicine.Dosage = request.Dosage;
        userMedicine.Frequency = request.Frequency;
        userMedicine.Duration = request.Duration;
        userMedicine.Notes = request.Notes;

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
            "Medicine updated successfully.");
    }
}
