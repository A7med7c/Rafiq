using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;

namespace Rafiq.Application.Features.LabReports.Commands.SaveLabReport;

public sealed record SaveLabReportCommand(
    Guid ProfileId,
    string? LabName,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl,
    List<SaveLabResultCommand>? Results)
    : IRequest<ApiResponse<LabReportResponseDto>>;

public sealed record SaveLabResultCommand(
    string? TestName,
    string? Value,
    string? Unit,
    string? NormalRange,
    string? Status);
