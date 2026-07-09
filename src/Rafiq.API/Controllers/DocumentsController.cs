using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;
using Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;
using Rafiq.Application.Features.LabReports.Commands.UploadLabReport;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Application.Features.LabReports.Queries.GetLabReportById;
using Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(
    IMediator mediator,
    ICurrentUserService currentUserService,
    ILabReportRepository labReportRepository,
    IImagingReportRepository imagingReportRepository,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpPost("upload/lab")]
    public async Task<IActionResult> UploadLabReport(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadLabReportCommand(image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("labs")]
    public async Task<IActionResult> SaveLabReport(
        [FromBody] SaveLabReportRequest body,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var reportDate = ParseDateOrToday(body.ReportDate);
        var labReport = new LabReport(
            userId,
            body.DoctorName ?? string.Empty,
            body.LabName ?? string.Empty,
            reportDate,
            body.ImageUrl ?? string.Empty,
            body.OcrText,
            body.Summary);

        foreach (var result in body.Results ?? [])
        {
            labReport.Results.Add(new LabResult
            {
                TestName = result.TestName ?? string.Empty,
                Value = result.Value ?? string.Empty,
                Unit = result.Unit ?? string.Empty,
                NormalRange = result.NormalRange ?? string.Empty,
                Status = result.Status
            });
        }

        await labReportRepository.AddAsync(labReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<LabReportResponseDto>.SuccessResponse(ToDto(labReport), "Lab report saved successfully."));
    }

    [HttpGet("labs")]
    public async Task<IActionResult> GetMyLabReports(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyLabReportsQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("labs/{id:guid}")]
    public async Task<IActionResult> GetLabReportById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetLabReportByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("upload/imaging")]
    public async Task<IActionResult> UploadImagingReport(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadImagingReportCommand(image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("imaging")]
    public async Task<IActionResult> SaveImagingReport(
        [FromBody] SaveImagingReportRequest body,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var imagingReport = new ImagingReport(
            userId,
            body.ImagingType ?? "Unknown",
            body.BodyPart ?? "Unknown",
            body.Findings ?? string.Empty,
            body.Impression ?? string.Empty,
            body.DoctorName,
            ParseDateOrToday(body.ReportDate),
            body.ImageUrl ?? string.Empty,
            body.OcrText,
            body.Summary);

        await imagingReportRepository.AddAsync(imagingReport, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<ImagingReportResponseDto>.SuccessResponse(ToDto(imagingReport), "Imaging report saved successfully."));
    }

    [HttpGet("imaging")]
    public async Task<IActionResult> GetMyImagingReports(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyImagingReportsQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("imaging/{id:guid}")]
    public async Task<IActionResult> GetImagingReportById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetImagingReportByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    private static DateOnly ParseDateOrToday(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

    private static LabReportResponseDto ToDto(LabReport report) =>
        new()
        {
            Id = report.Id,
            LabName = report.LabName,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            OCRText = report.OCRText,
            Summary = report.Description,
            ImageUrl = report.ImageUrl,
            CreatedAt = report.CreatedAt,
            Results = report.Results.Select(r => new LabResultResponseDto
            {
                Id = r.Id,
                TestName = r.TestName,
                Value = r.Value,
                Unit = r.Unit,
                NormalRange = r.NormalRange,
                Status = r.Status
            }).ToList()
        };

    private static ImagingReportResponseDto ToDto(ImagingReport report) =>
        new()
        {
            Id = report.Id,
            ImagingType = report.ImagingType,
            BodyPart = report.BodyPart,
            Findings = report.Findings,
            Impression = report.Impression,
            DoctorName = report.DoctorName,
            ReportDate = report.ReportDate.ToString("yyyy-MM-dd"),
            ImageUrl = report.ImageUrl,
            OCRText = report.OCRText,
            Summary = report.Description,
            CreatedAt = report.CreatedAt
        };
}

public sealed record SaveLabReportRequest(
    string? LabName,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl,
    List<SaveLabResultRequest>? Results);

public sealed record SaveLabResultRequest(
    string? TestName,
    string? Value,
    string? Unit,
    string? NormalRange,
    string? Status);

public sealed record SaveImagingReportRequest(
    string? ImagingType,
    string? BodyPart,
    string? Findings,
    string? Impression,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl);
