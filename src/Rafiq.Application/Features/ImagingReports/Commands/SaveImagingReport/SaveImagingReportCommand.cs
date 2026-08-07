using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;

namespace Rafiq.Application.Features.ImagingReports.Commands.SaveImagingReport;

public sealed record SaveImagingReportCommand(
    Guid ProfileId,
    string? ImagingType,
    string? BodyPart,
    string? Findings,
    string? Impression,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl,
    string? MedicalAttentionReason,
    string? RecommendedSpecialty,
    double? ConfidenceScore)
    : IRequest<ApiResponse<ImagingReportResponseDto>>;
