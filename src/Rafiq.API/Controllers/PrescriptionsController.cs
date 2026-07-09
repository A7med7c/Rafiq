using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.Commands.DeletePrescription;
using Rafiq.Application.Features.Prescriptions.Commands.UpdatePrescription;
using Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;
using Rafiq.Application.Features.Prescriptions.Queries.GetPrescriptionById;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/prescriptions")]
public sealed class PrescriptionsController(
    IMediator mediator,
    ICurrentUserService currentUserService,
    IPrescriptionRepository prescriptionRepository,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadPrescription(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadPrescriptionCommand(image),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SavePrescription(
        [FromBody] SavePrescriptionRequest body,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var prescription = new Prescription(
            userId,
            body.DoctorName ?? string.Empty,
            body.PatientName ?? string.Empty,
            ParseDateOrToday(body.PrescriptionDate),
            body.ImagePath ?? string.Empty);

        foreach (var medicine in body.Medicines ?? [])
        {
            prescription.Medicines.Add(new PrescriptionMedicine
            {
                MedicineName = medicine.MedicineName ?? string.Empty,
                Dosage = medicine.Dosage ?? string.Empty,
                Frequency = medicine.Frequency ?? string.Empty,
                Duration = medicine.Duration ?? string.Empty,
                Notes = medicine.Instructions ?? medicine.Notes
            });
        }

        await prescriptionRepository.AddAsync(prescription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<PrescriptionResponseDto>.SuccessResponse(ToDto(prescription), "Prescription saved successfully."));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyPrescriptions(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMyPrescriptionsQuery(),
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

    private static DateOnly ParseDateOrToday(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

    private static PrescriptionResponseDto ToDto(Prescription prescription) =>
        new()
        {
            Id = prescription.Id,
            DoctorName = prescription.DoctorName,
            PatientName = prescription.PatientName,
            PrescriptionDate = prescription.PrescriptionDate.ToString("yyyy-MM-dd"),
            ImagePath = prescription.ImagePath,
            CreatedAt = prescription.CreatedAt,
            Medicines = prescription.Medicines.Select(m => new PrescriptionMedicineResponseDto
            {
                Id = m.Id,
                MedicineName = m.MedicineName,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes
            }).ToList()
        };
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
