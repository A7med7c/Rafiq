using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.UpdateLabReport;

public sealed class UpdateLabReportCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    ILabReportRepository labReportRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        UpdateLabReportCommand request,
        CancellationToken cancellationToken)
    {
        var labReport = await labReportRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(LabReport), request.Id);

        await authorizationService.EnsureCanWriteAsync(labReport.UserHealthProfileId, cancellationToken);

        var reportDate = DateOnly.TryParseExact(
            request.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : labReport.ReportDate;

        labReport.Update(
            request.DoctorName ?? string.Empty,
            request.LabName ?? string.Empty,
            reportDate,
            request.Summary,
            request.ImageUrl,
            request.OcrText);

        labReport.Results.Clear();

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

        labReportRepository.Update(labReport);
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

        return ApiResponse<LabReportResponseDto>.SuccessResponse(dto, "Lab report updated successfully.");
    }
}