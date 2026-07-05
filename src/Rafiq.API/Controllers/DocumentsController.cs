using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.LabReports.Commands.UploadLabReport;
using Rafiq.Application.Features.LabReports.Queries.GetLabReportById;
using Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;
using Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;
using Rafiq.Application.Features.ImagingReports.Queries.GetImagingReportById;
using Rafiq.Application.Features.ImagingReports.Queries.GetMyImagingReports;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    #region Lab Reports

    /// <summary>
    /// Upload a lab report image, analyze it with AI, and save the results.
    /// </summary>
    [HttpPost("upload/lab")]
    public async Task<IActionResult> UploadLabReport(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadLabReportCommand(image),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get all lab reports for the currently authenticated user.
    /// </summary>
    [HttpGet("labs")]
    public async Task<IActionResult> GetMyLabReports(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyLabReportsQuery(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get a single lab report by its ID.
    /// Only returns the report if it belongs to the authenticated user.
    /// </summary>
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

    #endregion

    #region Imaging Reports

    /// <summary>
    /// Upload an imaging report image, analyze it with AI, and save the results.
    /// </summary>
    [HttpPost("upload/imaging")]
    public async Task<IActionResult> UploadImagingReport(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadImagingReportCommand(image),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get all imaging reports for the currently authenticated user.
    /// </summary>
    [HttpGet("imaging")]
    public async Task<IActionResult> GetMyImagingReports(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyImagingReportsQuery(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get a single imaging report by its ID.
    /// Only returns the report if it belongs to the authenticated user.
    /// </summary>
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

    #endregion
}
