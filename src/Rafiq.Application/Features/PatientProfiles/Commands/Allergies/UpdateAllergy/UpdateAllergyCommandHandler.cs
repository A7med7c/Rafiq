using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.UpdateAllergy;

public sealed class UpdateAllergyCommandHandler(
    IAllergyRepository allergyRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IRequestHandler<UpdateAllergyCommand, ApiResponse<AllergyDto>>
{
    public async Task<ApiResponse<AllergyDto>> Handle(
        UpdateAllergyCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.PatientProfileId, cancellationToken);

        var allergy = await allergyRepository.GetByIdAsync(request.AllergyId, cancellationToken)
            ?? throw new NotFoundException("Allergy", request.AllergyId);

        if (allergy.UserHealthProfileId != request.PatientProfileId)
            throw new NotFoundException("Allergy", request.AllergyId);

        var duplicate = await allergyRepository.ExistsByNameForProfileAsync(
            request.PatientProfileId, request.Name, excludeId: request.AllergyId, cancellationToken);

        if (duplicate)
            throw new ConflictException($"An allergy named '{request.Name}' already exists for this profile.");

        allergy.Update(request.Name, request.Severity);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AllergyDto>.SuccessResponse(
            mapper.Map<AllergyDto>(allergy),
            "Allergy updated successfully.");
    }
}
