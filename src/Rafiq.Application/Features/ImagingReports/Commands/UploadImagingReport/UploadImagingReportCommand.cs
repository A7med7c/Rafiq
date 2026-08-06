using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;

namespace Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;

public sealed record UploadImagingReportCommand(Guid ProfileId, IFormFile Image, string Language = "en", bool BypassFamilyDuplicateCheck = false)
    : IRequest<ApiResponse<ImagingReportResponseDto>>, IAiCommand;
