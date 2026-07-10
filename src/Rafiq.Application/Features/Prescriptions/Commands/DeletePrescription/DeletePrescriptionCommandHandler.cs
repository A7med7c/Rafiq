using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Prescriptions.Commands.DeletePrescription;

public sealed class DeletePrescriptionCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IPrescriptionRepository prescriptionRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePrescriptionCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeletePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var prescription = await prescriptionRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Prescription), request.Id);

        await authorizationService.EnsureCanWriteAsync(prescription.UserHealthProfileId, cancellationToken);

        prescriptionRepository.Delete(prescription);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Prescription deleted successfully.");
    }
}
