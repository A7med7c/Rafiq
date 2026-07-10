using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.SaveImagingReport;

public sealed class SaveImagingReportCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IImagingReportRepository imagingReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SaveImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        SaveImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.ProfileId, cancellationToken);

        var reportDate = DateOnly.TryParseExact(request.ReportDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var imagingReport = new ImagingReport(
            request.ProfileId,
            request.ImagingType ?? "Unknown",
            request.BodyPart ?? "Unknown",
            request.Findings ?? string.Empty,
            request.Impression ?? string.Empty,
            request.DoctorName,
            reportDate,
            request.ImageUrl ?? string.Empty,
            request.OcrText,
            request.Summary);

        await imagingReportRepository.AddAsync(imagingReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ImagingReportResponseDto
        {
            Id = imagingReport.Id,
            ImagingType = imagingReport.ImagingType,
            BodyPart = imagingReport.BodyPart,
            Findings = imagingReport.Findings,
            Impression = imagingReport.Impression,
            DoctorName = imagingReport.DoctorName,
            ReportDate = imagingReport.ReportDate.ToString("yyyy-MM-dd"),
            ImageUrl = imagingReport.ImageUrl,
            OCRText = imagingReport.OCRText,
            Summary = imagingReport.Description,
            CreatedAt = imagingReport.CreatedAt
        };

        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(dto, "Imaging report saved successfully.");
    }
}
