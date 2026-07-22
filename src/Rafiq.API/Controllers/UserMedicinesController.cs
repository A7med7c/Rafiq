using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.Commands.AddFromPrescription;
using Rafiq.Application.Features.UserMedicines.Commands.AddUserMedicine;
using Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;
using Rafiq.Application.Features.UserMedicines.Commands.ScanMedicineBox;
using Rafiq.Application.Features.UserMedicines.Commands.UpdateUserMedicine;
using Rafiq.Application.Features.UserMedicines.Queries.GetMyUserMedicines;
using Rafiq.Application.Features.UserMedicines.Queries.GetUserMedicineById;
using Rafiq.Domain.Enums;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user-medicines")]
public sealed class UserMedicinesController(
    IMediator mediator,
    IFileStorageService fileStorageService) : ControllerBase
{
    /// <summary>
    /// Upload a medicine box image without AI processing.
    /// Stores the image and returns its path for use when adding a medicine manually.
    /// Does NOT save to the database.
    /// </summary>
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedicineImage(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(image.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";

        using var stream = image.OpenReadStream();
        var imagePath = await fileStorageService.UploadFileAsync(
            stream, fileName, "medicine-boxes", cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(
            new { imagePath },
            "Image uploaded successfully."));
    }

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
        [FromQuery] Guid profileId,
        [FromBody] AddFromPrescriptionBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddFromPrescriptionCommand(profileId, body.PrescriptionMedicineIds),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Add a medicine to the user's list. 
    /// Source can be Manual (1), Prescription (2), or MedicineBox (3).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddUserMedicine(
        [FromQuery] Guid profileId,
        [FromBody] AddUserMedicineBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddUserMedicineCommand(
                profileId,
                body.MedicineName,
                body.Dosage,
                body.Frequency,
                body.Duration,
                body.Notes,
                body.ImagePath,
                body.Source),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get all medicines for the given profile.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyUserMedicines(
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyUserMedicinesQuery(profileId),
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

/// <summary>Request body for adding a UserMedicine manually or from scan.</summary>
public sealed record AddUserMedicineBody(
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string? Notes,
    string? ImagePath,
    MedicineSource Source);

/// <summary>Request body for adding medicines from a prescription.</summary>
public sealed record AddFromPrescriptionBody(List<Guid> PrescriptionMedicineIds);
