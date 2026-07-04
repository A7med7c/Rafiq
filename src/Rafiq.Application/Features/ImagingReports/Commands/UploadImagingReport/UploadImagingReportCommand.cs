using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;

namespace Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;

public sealed record UploadImagingReportCommand(IFormFile Image)
    : IRequest<ApiResponse<ImagingReportResponseDto>>;
