using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;

public sealed class GetMyImagingReportsQueryHandler(
    ICurrentUserService currentUserService,
    IImagingReportRepository imagingReportRepository)
    : IRequestHandler<GetMyImagingReportsQuery, ApiResponse<List<ImagingReportResponseDto>>>
{
    public async Task<ApiResponse<List<ImagingReportResponseDto>>> Handle(
        GetMyImagingReportsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var reports = await imagingReportRepository
            .GetAllByUserIdAsync(userId, cancellationToken);

        return ApiResponse<List<ImagingReportResponseDto>>.SuccessResponse(
            reports.Select(ImagingReportMapper.ToDto).ToList(),
            "Imaging reports retrieved successfully.");
    }
}
