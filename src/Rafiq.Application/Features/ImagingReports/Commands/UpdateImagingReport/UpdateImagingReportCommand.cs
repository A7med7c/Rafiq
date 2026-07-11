using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;

namespace Rafiq.Application.Features.ImagingReports.Commands.UpdateImagingReport;

public sealed record UpdateImagingReportCommand(
    Guid Id,
    string? ImagingType,
    string? BodyPart,
    string? Findings,
    string? Impression,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl)
    : IRequest<ApiResponse<ImagingReportResponseDto>>;