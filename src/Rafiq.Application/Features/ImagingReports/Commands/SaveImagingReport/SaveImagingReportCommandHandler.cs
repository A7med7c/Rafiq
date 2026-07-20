using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.SaveImagingReport;

public sealed class SaveImagingReportCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IImagingReportRepository imagingReportRepository,
    IProfileNotificationService profileNotificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SaveImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        SaveImagingReportCommand request,
        CancellationToken cancellationToken)
    {
        var profileId = request.ProfileId;

        if (profileId == Guid.Empty)
        {
            var currentUserId = currentUserService.UserId
                ?? throw new UnauthorizedException("Authentication is required.");

            profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
                ?? throw new NotFoundException("PatientProfile", currentUserId);
        }

        await authorizationService.EnsureCanWriteAsync(profileId, cancellationToken);

        var reportDate = DateOnly.TryParseExact(request.ReportDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var imagingType = request.ImagingType ?? "Unknown";
        var bodyPart = request.BodyPart ?? "Unknown";

        if (await imagingReportRepository.ExistsDuplicateAsync(profileId, reportDate, imagingType, bodyPart, cancellationToken))
            throw new ConflictException("This document already exists in your medical records.");

        var imagingReport = new ImagingReport(
            profileId,
            imagingType,
            bodyPart,
            request.Findings ?? string.Empty,
            request.Impression ?? string.Empty,
            request.DoctorName,
            reportDate,
            request.ImageUrl ?? string.Empty,
            request.OcrText,
            request.Summary);

        await imagingReportRepository.AddAsync(imagingReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify everyone who may view this profile that a new imaging report was added.
        await profileNotificationService.NotifyMedicalRecordAddedAsync(
            profileId,
            currentUserService.UserId ?? Guid.Empty,
            MedicalRecordKind.ImagingReport,
            cancellationToken);

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
