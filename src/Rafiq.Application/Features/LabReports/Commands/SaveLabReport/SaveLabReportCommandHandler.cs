using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.SaveLabReport;

public sealed class SaveLabReportCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    ILabReportRepository labReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SaveLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        SaveLabReportCommand request,
        CancellationToken cancellationToken)
    {
        await authorizationService.EnsureCanWriteAsync(request.ProfileId, cancellationToken);

        var reportDate = DateOnly.TryParseExact(request.ReportDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var labReport = new LabReport(
            request.ProfileId,
            request.DoctorName ?? string.Empty,
            request.LabName ?? string.Empty,
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
