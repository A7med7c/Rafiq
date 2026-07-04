using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;

namespace Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;

/// <summary>
/// Returns all Lab Reports that belong to the currently authenticated user.
/// </summary>
public sealed record GetMyLabReportsQuery
    : IRequest<ApiResponse<List<LabReportResponseDto>>>;
