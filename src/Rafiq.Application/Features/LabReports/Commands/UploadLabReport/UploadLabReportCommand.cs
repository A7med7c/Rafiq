using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;

namespace Rafiq.Application.Features.LabReports.Commands.UploadLabReport;

/// <summary>
/// Uploads a lab report image, analyzes it with Bedrock,
/// returns the extracted data for review.
/// UserId is NEVER accepted from the client — it comes from the JWT.
/// </summary>
public sealed record UploadLabReportCommand(Guid ProfileId, IFormFile Image, string Language = "en", bool BypassFamilyDuplicateCheck = false)
    : IRequest<ApiResponse<LabReportResponseDto>>, IAiCommand;
