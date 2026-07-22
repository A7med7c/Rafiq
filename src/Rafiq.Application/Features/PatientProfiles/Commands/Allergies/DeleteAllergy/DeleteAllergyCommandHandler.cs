using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.DeleteAllergy;

public sealed class DeleteAllergyCommandHandler(
    IAllergyRepository allergyRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAllergyCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        DeleteAllergyCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.PatientProfileId, cancellationToken);

        var allergy = await allergyRepository.GetByIdAsync(request.AllergyId, cancellationToken)
            ?? throw new NotFoundException("Allergy", request.AllergyId);

        if (allergy.UserHealthProfileId != request.PatientProfileId)
            throw new NotFoundException("Allergy", request.AllergyId);

        allergyRepository.Remove(allergy);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Allergy deleted successfully.");
    }
}
