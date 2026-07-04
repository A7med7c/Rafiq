using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;

namespace Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;

public sealed record GetImagingReportByIdQuery(Guid Id)
    : IRequest<ApiResponse<ImagingReportResponseDto>>;
