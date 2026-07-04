using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;

namespace Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;

public sealed record GetMyImagingReportsQuery()
    : IRequest<ApiResponse<List<ImagingReportResponseDto>>>;
