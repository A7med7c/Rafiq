using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.UpdateChronicDisease;

public sealed class UpdateChronicDiseaseCommandHandler(
    IChronicDiseaseRepository chronicDiseaseRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<UpdateChronicDiseaseCommand, ApiResponse<ChronicDiseaseDto>>
{
    public async Task<ApiResponse<ChronicDiseaseDto>> Handle(
        UpdateChronicDiseaseCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.PatientProfileId, cancellationToken);

        var disease = await chronicDiseaseRepository.GetByIdAsync(request.DiseaseId, cancellationToken)
            ?? throw new NotFoundException("ChronicDisease", request.DiseaseId);

        if (disease.UserHealthProfileId != request.PatientProfileId)
            throw new NotFoundException("ChronicDisease", request.DiseaseId);

        var duplicate = await chronicDiseaseRepository.ExistsByNameForProfileAsync(
            request.PatientProfileId, request.Name, excludeId: request.DiseaseId, cancellationToken);

        if (duplicate)
            throw new ConflictException($"A chronic disease named '{request.Name}' already exists for this profile.");

        disease.Update(request.Name, request.DiagnosedAt, request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.PatientProfileId, cancellationToken);

        return ApiResponse<ChronicDiseaseDto>.SuccessResponse(
            mapper.Map<ChronicDiseaseDto>(disease),
            "Chronic disease updated successfully.");
    }
}
