using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.DeleteGeneralDocument;

public sealed class DeleteGeneralDocumentCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IGeneralDocumentRepository repository,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<DeleteGeneralDocumentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteGeneralDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
            ?? throw new NotFoundException("PatientProfile", currentUserId);

        var document = await repository.GetByIdAsync(request.Id, profileId, cancellationToken)
            ?? throw new NotFoundException("GeneralDocument", request.Id);

        repository.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(profileId, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "General document deleted successfully.");
    }
}