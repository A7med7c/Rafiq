using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.SaveLabReport;

public sealed class SaveLabReportCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    ILabReportRepository labReportRepository,
    IProfileNotificationService profileNotificationService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SaveLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        SaveLabReportCommand request,
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

        var labName = request.LabName ?? string.Empty;
        var doctorName = request.DoctorName ?? string.Empty;

        if (await labReportRepository.ExistsDuplicateAsync(profileId, reportDate, labName, doctorName, cancellationToken))
            throw new ConflictException("This document already exists in your medical records.");

        var labReport = new LabReport(
            profileId,
            doctorName,
            labName,
            reportDate,
            request.ImageUrl ?? string.Empty,
            request.OcrText,
            request.Summary);

        foreach (var result in request.Results ?? [])
        {
            labReport.Results.Add(new LabResult
            {
                TestName = result.TestName ?? string.Empty,
                Value = result.Value ?? string.Empty,
                Unit = result.Unit ?? string.Empty,
                NormalRange = result.NormalRange ?? string.Empty,
                Status = result.Status
            });
        }

        await labReportRepository.AddAsync(labReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify everyone who may view this profile that a new lab report was added.
        await profileNotificationService.NotifyMedicalRecordAddedAsync(
            profileId,
            currentUserService.UserId ?? Guid.Empty,
            MedicalRecordKind.LabReport,
            cancellationToken);

        var dto = new LabReportResponseDto
        {
            Id = labReport.Id,
            LabName = labReport.LabName,
            DoctorName = labReport.DoctorName,
            ReportDate = labReport.ReportDate.ToString("yyyy-MM-dd"),
            OCRText = labReport.OCRText,
            Summary = labReport.Description,
            ImageUrl = labReport.ImageUrl,
            CreatedAt = labReport.CreatedAt,
            Results = labReport.Results.Select(r => new LabResultResponseDto
            {
                Id = r.Id,
                TestName = r.TestName,
                Value = r.Value,
                Unit = r.Unit,
                NormalRange = r.NormalRange,
                Status = r.Status
            }).ToList()
        };

        return ApiResponse<LabReportResponseDto>.SuccessResponse(dto, "Lab report saved successfully.");
    }
}
