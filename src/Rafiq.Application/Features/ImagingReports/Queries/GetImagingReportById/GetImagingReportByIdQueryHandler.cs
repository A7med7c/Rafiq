using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;

public sealed class GetImagingReportByIdQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    IImagingReportRepository imagingReportRepository)
    : IRequestHandler<GetImagingReportByIdQuery, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        GetImagingReportByIdQuery request,
        CancellationToken cancellationToken)
    {
        var report = await imagingReportRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.ImagingReport), request.Id);

        await authorizationService.EnsureCanReadAsync(report.UserHealthProfileId, cancellationToken);

        var dto = new ImagingReportResponseDto
        {
            Id = report.Id,
            ImagingType = report.ImagingType,
            BodyPart = report.BodyPart,
            Findings = report.Findings,
            Impression = report.Impression,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            ImageUrl = report.ImageUrl,
            OCRText = report.OCRText,
            Summary = report.Description,
            CreatedAt = report.CreatedAt
        };

        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(
            dto,
            "Imaging report retrieved successfully.");
    }
}
