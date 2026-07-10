using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.LabReports.Queries.GetLabReportById;

public sealed class GetLabReportByIdQueryHandler(
    IHealthProfileAuthorizationService authorizationService,
    ILabReportRepository labReportRepository)
    : IRequestHandler<GetLabReportByIdQuery, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        GetLabReportByIdQuery request,
        CancellationToken cancellationToken)
    {
        var report = await labReportRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.LabReport), request.Id);

        await authorizationService.EnsureCanReadAsync(report.UserHealthProfileId, cancellationToken);

        var dto = new LabReportResponseDto
        {
            Id = report.Id,
            LabName = report.LabName,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            OCRText = report.OCRText,
            Summary = report.Description,
            ImageUrl = report.ImageUrl,
            CreatedAt = report.CreatedAt,
            Results = report.Results.Select(r => new LabResultResponseDto
            {
                Id = r.Id,
                TestName = r.TestName,
                Value = r.Value,
                Unit = r.Unit,
                NormalRange = r.NormalRange,
                Status = r.Status
            }).ToList()
        };

        return ApiResponse<LabReportResponseDto>.SuccessResponse(
            dto,
            "Lab report retrieved successfully.");
    }
}
