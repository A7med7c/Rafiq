using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocument;
using Rafiq.Application.Features.ImagingReports.Commands.SaveImagingReport;
using Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;
using Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;
using Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;
using Rafiq.Application.Features.LabReports.Commands.SaveLabReport;
using Rafiq.Application.Features.LabReports.Commands.UploadLabReport;
using Rafiq.Application.Features.LabReports.Queries.GetLabReportById;
using Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    // ── Lab Reports ──────────────────────────────────────────────────────────

    [HttpPost("upload/lab")]
    public async Task<IActionResult> UploadLabReport(
        [FromQuery] Guid profileId,
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadLabReportCommand(profileId, image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("labs")]
    public async Task<IActionResult> SaveLabReport(
        [FromQuery] Guid profileId,
        [FromBody] SaveLabReportRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SaveLabReportCommand(
                profileId,
                body.LabName,
                body.DoctorName,
                body.ReportDate,
                body.Summary,
                body.OcrText,
                body.ImageUrl,
                body.Results?.Select(r => new SaveLabResultCommand(
                    r.TestName,
                    r.Value,
                    r.Unit,
                    r.NormalRange,
                    r.Status)).ToList()),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("labs")]
    public async Task<IActionResult> GetMyLabReports(
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyLabReportsQuery(profileId),
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

    // ── Imaging Reports ───────────────────────────────────────────────────────

    [HttpPost("upload/imaging")]
    public async Task<IActionResult> UploadImagingReport(
        [FromQuery] Guid profileId,
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadImagingReportCommand(profileId, image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("imaging")]
    public async Task<IActionResult> SaveImagingReport(
        [FromQuery] Guid profileId,
        [FromBody] SaveImagingReportRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SaveImagingReportCommand(
                profileId,
                body.ImagingType,
                body.BodyPart,
                body.Findings,
                body.Impression,
                body.DoctorName,
                body.ReportDate,
                body.Summary,
                body.OcrText,
                body.ImageUrl),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("imaging")]
    public async Task<IActionResult> GetMyImagingReports(
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyImagingReportsQuery(profileId),
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

    // ── General Documents ─────────────────────────────────────────────────────

    [HttpPost("general/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadGeneralDocument(
        [FromForm] UploadGeneralDocumentCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(result);
    }
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

