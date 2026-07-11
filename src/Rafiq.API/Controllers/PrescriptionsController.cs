using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Features.Prescriptions.Commands.DeletePrescription;
using Rafiq.Application.Features.Prescriptions.Commands.UpdatePrescription;
using Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;
using Rafiq.Application.Features.Prescriptions.Queries.GetPrescriptionById;
using Rafiq.Application.Features.Prescriptions.Commands.SavePrescription;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/prescriptions")]
public sealed class PrescriptionsController(IMediator mediator) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadPrescription(
        [FromQuery] Guid profileId,
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadPrescriptionCommand(profileId, image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SavePrescription(
        [FromQuery] Guid profileId,
        [FromBody] SavePrescriptionRequest body,
        CancellationToken cancellationToken)
    {
        var medicines = body.Medicines?
            .Select(m => new SavePrescriptionMedicineCommandItem(
                m.MedicineName, m.Dosage, m.Frequency, m.Duration, m.Instructions, m.Notes))
            .ToList();

        var result = await mediator.Send(
            new SavePrescriptionCommand(
                profileId,
                body.DoctorName,
                body.PatientName,
                body.PrescriptionDate,
                body.ImagePath,
                medicines),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyPrescriptions(
        [FromQuery] Guid profileId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyPrescriptionsQuery(profileId),
            cancellationToken);

        return Ok(result);
    }

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

public sealed record SavePrescriptionRequest(
    string? DoctorName,
    string? PatientName,
    string? PrescriptionDate,
    string? ImagePath,
    List<SavePrescriptionMedicineRequest>? Medicines);

public sealed record SavePrescriptionMedicineRequest(
    string? MedicineName,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? Instructions,
    string? Notes);

public sealed record UpdatePrescriptionRequest(
    string DoctorName,
    string PatientName,
    string PrescriptionDate);
