using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;

public sealed class GetMyLabReportsQueryHandler(
    ICurrentUserService currentUserService,
    ILabReportRepository labReportRepository)
    : IRequestHandler<GetMyLabReportsQuery, ApiResponse<List<LabReportResponseDto>>>
{
    public async Task<ApiResponse<List<LabReportResponseDto>>> Handle(
        GetMyLabReportsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var reports = await labReportRepository
            .GetAllByUserIdAsync(userId, cancellationToken);

        var dtos = reports.Select(report => new LabReportResponseDto
        {
            Id = report.Id,
            LabName = report.LabName,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            //ImageUrl   = report.ImageUrl,
            OCRText = report.OCRText,
            Summary = report.Description,
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
        }).ToList();

        return ApiResponse<List<LabReportResponseDto>>.SuccessResponse(
            dtos,
            "Lab reports retrieved successfully.");
    }
}
