using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;

namespace Rafiq.Application.Features.LabReports.Queries.GetLabReportById;

/// <summary>
/// Returns a single Lab Report by its Id.
/// Only succeeds if the report belongs to the currently authenticated user.
/// </summary>
public sealed record GetLabReportByIdQuery(Guid Id)
    : IRequest<ApiResponse<LabReportResponseDto>>;
