using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.ImagingReports.Commands.DeleteImagingReport;

public sealed class DeleteImagingReportCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IImagingReportRepository imagingReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteImagingReportCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        var imagingReport = await imagingReportRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ImagingReport), request.Id);

        await authorizationService.EnsureCanWriteAsync(imagingReport.UserHealthProfileId, cancellationToken);

        imagingReportRepository.Remove(imagingReport);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true, "Imaging report deleted successfully.");
    }
}