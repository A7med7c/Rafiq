using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.UpdateImagingReport;

public sealed class UpdateImagingReportCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IImagingReportRepository imagingReportRepository,
    IUnitOfWork unitOfWork,
    IHealthSummaryCacheRepository summaryCache)
    : IRequestHandler<UpdateImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        UpdateImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        var imagingReport = await imagingReportRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ImagingReport), request.Id);

        await authorizationService.EnsureCanWriteAsync(imagingReport.UserHealthProfileId, cancellationToken);

        var reportDate = DateOnly.TryParseExact(
            request.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : imagingReport.ReportDate;

        imagingReport.Update(
            request.ImagingType ?? string.Empty,
            request.BodyPart ?? string.Empty,
            request.Findings ?? string.Empty,
            request.Impression ?? string.Empty,
            request.DoctorName,
            reportDate,
            request.Summary,
            imagingReport.MedicalAttentionReason,
            imagingReport.RecommendedSpecialty,
            imagingReport.ConfidenceScore,
            request.ImageUrl,
            request.OcrText);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await summaryCache.MarkNeedsRefreshAsync(imagingReport.UserHealthProfileId, cancellationToken);

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

        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(dto, "Imaging report updated successfully.");
    }
}