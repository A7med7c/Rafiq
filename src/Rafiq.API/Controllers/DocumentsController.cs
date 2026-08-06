using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocument;
using Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocumentAsync;
using Rafiq.Application.Features.GeneralDocuments.Commands.RetryDocumentAnalysis;
using Rafiq.Application.Features.GeneralDocuments.Queries.GetGeneralDocumentStatus;
using Rafiq.Application.Features.GeneralDocuments.Commands.DeleteGeneralDocument;
using Rafiq.Application.Features.GeneralDocuments.Commands.SaveGeneralDocument;
using Rafiq.Application.Features.GeneralDocuments.Commands.UpdateGeneralDocument;
using Rafiq.Application.Features.GeneralDocuments.Queries.GetMyGeneralDocuments;
using Rafiq.Application.Features.ImagingReports.Commands.SaveImagingReport;
using Rafiq.Application.Features.ImagingReports.Commands.DeleteImagingReport;
using Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;
using Rafiq.Application.Features.ImagingReports.Commands.UpdateImagingReport;
using Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;
using Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;
using Rafiq.Application.Features.LabReports.Commands.DeleteLabReport;
using Rafiq.Application.Features.LabReports.Commands.SaveLabReport;
using Rafiq.Application.Features.LabReports.Commands.UploadLabReport;
using Rafiq.Application.Features.LabReports.Commands.UpdateLabReport;
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
        [FromHeader(Name = "Accept-Language")] string? language,
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadLabReportCommand(profileId, image, language ?? "en"),
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

    [HttpPut("labs/{id:guid}")]
    public async Task<IActionResult> UpdateLabReport(
        Guid id,
        [FromBody] UpdateLabReportRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateLabReportCommand(
                id,
                body.LabName,
                body.DoctorName,
                body.ReportDate,
                body.Summary,
                body.OcrText,
                body.ImageUrl,
                body.Results?.Select(r => new UpdateLabResultCommandItem(
                    r.TestName,
                    r.Value,
                    r.Unit,
                    r.NormalRange,
                    r.Status)).ToList()),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("labs/{id:guid}")]
    public async Task<IActionResult> DeleteLabReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteLabReportCommand(id), cancellationToken);
        return Ok(result);
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
        [FromHeader(Name = "Accept-Language")] string? language,
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadImagingReportCommand(profileId, image, language ?? "en"),
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

    [HttpPut("imaging/{id:guid}")]
    public async Task<IActionResult> UpdateImagingReport(
        Guid id,
        [FromBody] UpdateImagingReportRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateImagingReportCommand(
                id,
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

        return Ok(result);
    }

    [HttpDelete("imaging/{id:guid}")]
    public async Task<IActionResult> DeleteImagingReport(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteImagingReportCommand(id), cancellationToken);
        return Ok(result);
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

    [HttpGet("general")]
    public async Task<IActionResult> GetMyGeneralDocuments(
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyGeneralDocumentsQuery(profileId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("general")]
    public async Task<IActionResult> SaveGeneralDocument(
        [FromQuery] Guid profileId,
        [FromBody] SaveGeneralDocumentRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SaveGeneralDocumentCommand(
                profileId,
                body.Title,
                body.Description,
                body.AiSummary,
                body.ImagePath,
                body.DocumentType,
                body.DoctorName,
                body.HospitalOrClinic,
                body.DocumentDate,
                body.OcrText),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("general/{id:guid}")]
    public async Task<IActionResult> UpdateGeneralDocument(
        Guid id,
        [FromBody] UpdateGeneralDocumentRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateGeneralDocumentCommand(
                id,
                body.Title,
                body.Description,
                body.AiSummary,
                body.ImagePath,
                body.DocumentType,
                body.DoctorName,
                body.HospitalOrClinic,
                body.DocumentDate,
                body.OcrText),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("general/{id:guid}")]
    public async Task<IActionResult> DeleteGeneralDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteGeneralDocumentCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Async upload: saves the image immediately and queues AI analysis as a background job.
    /// Returns a documentId — the client polls GET /general or listens to SignalR for completion.
    /// </summary>
    [HttpPost("general/upload-async")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadGeneralDocumentAsync(
        [FromQuery] Guid profileId,
        IFormFile image,
        [FromForm] string? description,
        [FromHeader(Name = "Accept-Language")] string? language,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadGeneralDocumentAsyncCommand(image, profileId, description, language ?? "en"),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns the current analysis status of a single general document.
    /// Used as a polling fallback when SignalR is unavailable.
    /// </summary>
    [HttpGet("general/status/{id:guid}")]
    public async Task<IActionResult> GetGeneralDocumentStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetGeneralDocumentStatusQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Re-queues a Failed document for AI analysis. Returns 400 if the document is not in Failed state.
    /// </summary>
    [HttpPost("general/{id:guid}/retry")]
    public async Task<IActionResult> RetryDocumentAnalysis(
        Guid id,
        [FromHeader(Name = "Accept-Language")] string? language,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RetryDocumentAnalysisCommand(id, language ?? "en"), cancellationToken);
        return Ok(result);
    }

    // ── Manual image upload (no AI) ───────────────────────────────────────────

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadRecordImage(
        IFormFile image,
        [FromServices] IFileStorageService fileStorageService,
        CancellationToken cancellationToken)
    {
        if (image == null || image.Length == 0)
            return BadRequest(ApiResponse<object>.FailureResponse("No image provided."));

        var ext = Path.GetExtension(image.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        using var stream = image.OpenReadStream();
        var path = await fileStorageService.UploadFileAsync(stream, fileName, "manual", cancellationToken);
        return Ok(ApiResponse<UploadRecordImageResponse>.SuccessResponse(new UploadRecordImageResponse(path)));
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

public sealed record UpdateLabReportRequest(
    string? LabName,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl,
    List<SaveLabResultRequest>? Results);

public sealed record UpdateImagingReportRequest(
    string? ImagingType,
    string? BodyPart,
    string? Findings,
    string? Impression,
    string? DoctorName,
    string? ReportDate,
    string? Summary,
    string? OcrText,
    string? ImageUrl);

public sealed record SaveGeneralDocumentRequest(
    string Title,
    string Description,
    string? AiSummary,
    string ImagePath,
    string? DocumentType,
    string? DoctorName,
    string? HospitalOrClinic,
    string? DocumentDate,
    string? OcrText);

public sealed record UpdateGeneralDocumentRequest(
    string Title,
    string Description,
    string? AiSummary,
    string? ImagePath,
    string? DocumentType,
    string? DoctorName,
    string? HospitalOrClinic,
    string? DocumentDate,
    string? OcrText);

public sealed record UploadRecordImageResponse(string Path);

