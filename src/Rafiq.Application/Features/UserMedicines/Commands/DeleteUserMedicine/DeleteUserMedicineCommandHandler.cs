using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;

public sealed class DeleteUserMedicineCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserMedicineCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteUserMedicineCommand request,
        CancellationToken cancellationToken)
    {
        var userMedicine = await userMedicineRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.UserMedicine), request.Id);

        await authorizationService.EnsureCanWriteAsync(userMedicine.UserHealthProfileId, cancellationToken);

        userMedicineRepository.Delete(userMedicine);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Medicine deleted successfully.");
    }
}
