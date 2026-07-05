using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.LabReports.Commands.UploadLabReport;
using Rafiq.Application.Features.LabReports.Queries.GetLabReportById;
using Rafiq.Application.Features.LabReports.Queries.GetMyLabReports;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
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
}
