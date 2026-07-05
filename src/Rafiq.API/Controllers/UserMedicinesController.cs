using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.UserMedicines.Commands.AddFromPrescription;
using Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;
using Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;
using Rafiq.Application.Features.UserMedicines.Commands.ScanMedicineBox;
using Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;
using Rafiq.Application.Features.UserMedicines.Queries.GetMyUserMedicines;
using Rafiq.Application.Features.UserMedicines.Queries.GetUserMedicineById;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user-medicines")]
public sealed class UserMedicinesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Upload a medicine box image and extract details via AI.
    /// Does NOT save to the database. The client should present the data for review.
    /// </summary>
    [HttpPost("scan-box")]
    public async Task<IActionResult> ScanMedicineBox(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ScanMedicineBoxCommand(image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("from-prescription")]
    public async Task<IActionResult> AddFromPrescription(
        [FromBody] AddFromPrescriptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Add a medicine to the user's list. 
    /// Source can be Manual (1), Prescription (2), or MedicineBox (3).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddUserMedicine(
        [FromBody] AddUserMedicineCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get all medicines for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyUserMedicines(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyUserMedicinesQuery(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific medicine by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserMedicineById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetUserMedicineByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Update an existing medicine.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUserMedicine(
        Guid id,
        [FromBody] UpdateUserMedicineRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateUserMedicineCommand(
                id,
                body.MedicineName,
                body.Dosage,
                body.Frequency,
                body.Duration,
                body.Notes),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a medicine.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUserMedicine(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DeleteUserMedicineCommand(id),
            cancellationToken);

        return Ok(result);
    }
}

/// <summary>Request body for updating a UserMedicine.</summary>
public sealed record UpdateUserMedicineRequest(
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string? Notes);
