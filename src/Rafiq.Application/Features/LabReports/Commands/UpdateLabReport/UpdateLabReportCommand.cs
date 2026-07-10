using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;

namespace Rafiq.Application.Features.LabReports.Commands.UpdateLabReport;

public sealed record UpdateLabReportCommand(
    Guid Id,
    string? LabName,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl,
    List<UpdateLabResultCommandItem>? Results)
    : IRequest<ApiResponse<LabReportResponseDto>>;

public sealed record UpdateLabResultCommandItem(
    string? TestName,
    string? Value,
    string? Unit,
    string? NormalRange,
    string? Status);