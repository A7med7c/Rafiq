using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.LabReports.Commands.DeleteLabReport;

public sealed class DeleteLabReportCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    ILabReportRepository labReportRepository,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<DeleteLabReportCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteLabReportCommand request,
        CancellationToken cancellationToken)
    {
        var labReport = await labReportRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LabReport), request.Id);

        await authorizationService.EnsureCanWriteAsync(labReport.UserHealthProfileId, cancellationToken);

        var profileId = labReport.UserHealthProfileId;
        labReportRepository.Remove(labReport);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(profileId, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Lab report deleted successfully.");
    }
}