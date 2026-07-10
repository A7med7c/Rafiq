using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;

public sealed class GetMyImagingReportsQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    IImagingReportRepository imagingReportRepository)
    : IRequestHandler<GetMyImagingReportsQuery, ApiResponse<List<ImagingReportResponseDto>>>
{
    public async Task<ApiResponse<List<ImagingReportResponseDto>>> Handle(
        GetMyImagingReportsQuery request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanReadAsync(request.ProfileId, cancellationToken);

        var reports = await imagingReportRepository
            .GetAllByProfileIdAsync(request.ProfileId, cancellationToken);

        var dtos = reports.Select(report => new ImagingReportResponseDto
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
        }).ToList();

        return ApiResponse<List<ImagingReportResponseDto>>.SuccessResponse(
            dtos,
            "Imaging reports retrieved successfully.");
    }
}
