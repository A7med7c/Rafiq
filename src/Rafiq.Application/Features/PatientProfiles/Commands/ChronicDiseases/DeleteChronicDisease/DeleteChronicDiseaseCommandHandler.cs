using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.PatientProfiles.Commands.ChronicDiseases.DeleteChronicDisease;

public sealed class DeleteChronicDiseaseCommandHandler(
    IChronicDiseaseRepository chronicDiseaseRepository,
    IHealthProfileAuthorizationService authorizationService,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<DeleteChronicDiseaseCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        DeleteChronicDiseaseCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.PatientProfileId, cancellationToken);

        var disease = await chronicDiseaseRepository.GetByIdAsync(request.DiseaseId, cancellationToken)
            ?? throw new NotFoundException("ChronicDisease", request.DiseaseId);

        if (disease.UserHealthProfileId != request.PatientProfileId)
            throw new NotFoundException("ChronicDisease", request.DiseaseId);

        chronicDiseaseRepository.Remove(disease);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(request.PatientProfileId, cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Chronic disease deleted successfully.");
    }
}
