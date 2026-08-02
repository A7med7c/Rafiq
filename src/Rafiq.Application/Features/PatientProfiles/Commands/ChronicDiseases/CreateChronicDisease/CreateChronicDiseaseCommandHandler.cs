using MapsterMapper;
using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.PatientProfiles.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.CreateChronicDisease;

public sealed class CreateChronicDiseaseCommandHandler(
    IChronicDiseaseRepository chronicDiseaseRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<CreateChronicDiseaseCommand, ApiResponse<ChronicDiseaseDto>>
{
    public async Task<ApiResponse<ChronicDiseaseDto>> Handle(
        CreateChronicDiseaseCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.PatientProfileId, cancellationToken);

        var duplicate = await chronicDiseaseRepository.ExistsByNameForProfileAsync(
            request.PatientProfileId, request.Name, excludeId: null, cancellationToken);

        if (duplicate)
            throw new ConflictException($"A chronic disease named '{request.Name}' already exists for this profile.");

        var disease = new ChronicDisease(request.Name, request.DiagnosedAt, request.Status)
        {
            UserHealthProfileId = request.PatientProfileId
        };

        await chronicDiseaseRepository.AddAsync(disease, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.PatientProfileId, cancellationToken);

        return ApiResponse<ChronicDiseaseDto>.SuccessResponse(
            mapper.Map<ChronicDiseaseDto>(disease),
            "Chronic disease created successfully.");
    }
}
