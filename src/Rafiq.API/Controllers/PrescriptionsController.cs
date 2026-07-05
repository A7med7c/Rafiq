using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Prescriptions.Commands.DeletePrescription;
using Rafiq.Application.Features.Prescriptions.Commands.UpdatePrescription;
using Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;
using Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;
using Rafiq.Application.Features.Prescriptions.Queries.GetPrescriptionById;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/prescriptions")]
public sealed class PrescriptionsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Upload a prescription image, analyze it with AI, and save the results.
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadPrescription(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadPrescriptionCommand(image),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get all prescriptions for the currently authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyPrescriptions(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyPrescriptionsQuery(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get a single prescription by its ID.
    /// Only returns the prescription if it belongs to the authenticated user.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPrescriptionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetPrescriptionByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Update the editable fields of a prescription (DoctorName, PatientName, PrescriptionDate).
    /// Only updates if the prescription belongs to the authenticated user.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePrescription(
        Guid id,
        [FromBody] UpdatePrescriptionRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdatePrescriptionCommand(
                id,
                body.DoctorName,
                body.PatientName,
                body.PrescriptionDate),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a prescription by its ID.
    /// Only deletes if the prescription belongs to the authenticated user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePrescription(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DeletePrescriptionCommand(id),
            cancellationToken);

        return Ok(result);
    }
}

/// <summary>Request body for UpdatePrescription endpoint.</summary>
public sealed record UpdatePrescriptionRequest(
    string DoctorName,
    string PatientName,
    string PrescriptionDate);
