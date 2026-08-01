using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.GeneralDocuments.Commands.RetryDocumentAnalysis;

public sealed class RetryDocumentAnalysisCommandHandler(
    ICurrentUserService currentUserService,
    IGeneralDocumentRepository repository,
    IDocumentAnalysisJobService analysisJobService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RetryDocumentAnalysisCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        RetryDocumentAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var document = await repository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new NotFoundException("GeneralDocument", request.DocumentId);

        if (document.AnalysisStatus != GeneralDocumentStatus.Failed)
            return ApiResponse<bool>.FailureResponse("Document is not in a retryable state.");

        // Atomically reset to Pending — the job checks for Pending before claiming
        document.ResetForRetry();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Enqueue after the DB write — prevents race where job runs before status is Pending
        analysisJobService.EnqueueAnalysis(document.Id, userId, document.UserHealthProfileId);

        return ApiResponse<bool>.SuccessResponse(true, "Document queued for retry.");
    }
}
