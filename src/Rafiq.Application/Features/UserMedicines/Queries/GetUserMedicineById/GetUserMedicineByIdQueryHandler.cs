using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Queries.GetUserMedicineById;

public sealed class GetUserMedicineByIdQueryHandler(
    ICurrentUserService currentUserService,
    IUserMedicineRepository userMedicineRepository)
    : IRequestHandler<GetUserMedicineByIdQuery, ApiResponse<UserMedicineResponseDto>>
{
    public async Task<ApiResponse<UserMedicineResponseDto>> Handle(
        GetUserMedicineByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var userMedicine = await userMedicineRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.UserMedicine), request.Id);

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
            "Medicine retrieved successfully.");
    }
}
