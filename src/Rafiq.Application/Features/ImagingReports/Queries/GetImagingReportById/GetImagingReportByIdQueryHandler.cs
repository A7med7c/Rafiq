using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;

public sealed class GetImagingReportByIdQueryHandler(
    ICurrentUserService currentUserService,
    IImagingReportRepository imagingReportRepository)
    : IRequestHandler<GetImagingReportByIdQuery, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        GetImagingReportByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var report = await imagingReportRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.ImagingReport), request.Id);

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
