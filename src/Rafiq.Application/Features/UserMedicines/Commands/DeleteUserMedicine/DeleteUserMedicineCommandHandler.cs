using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;

public sealed class DeleteUserMedicineCommandHandler(
    ICurrentUserService currentUserService,
    IUserMedicineRepository userMedicineRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserMedicineCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteUserMedicineCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var deleted = await userMedicineRepository.DeleteAsync(
            request.Id,
            userId,
            cancellationToken);

        if (!deleted)
            throw new NotFoundException(nameof(Domain.Entities.Documents.UserMedicine), request.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Medicine deleted successfully.");
    }
}
