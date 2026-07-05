using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Prescriptions.Commands.DeletePrescription;

public sealed class DeletePrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IPrescriptionRepository prescriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePrescriptionCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeletePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var deleted = await prescriptionRepository.DeleteAsync(
            request.Id,
            userId,
            cancellationToken);

        if (!deleted)
            throw new NotFoundException(nameof(Domain.Entities.Documents.Prescription), request.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Prescription deleted successfully.");
    }
}
