using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;

public sealed class UploadPrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IPrescriptionRepository prescriptionRepository,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadPrescriptionCommand, ApiResponse<PrescriptionResponseDto>>
{
    public async Task<ApiResponse<PrescriptionResponseDto>> Handle(
        UploadPrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Resolve the authenticated user ──────────────────────────────
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // ── 2. Save physical file via IFileStorageService ─────────────────
        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var imageStream = request.Image.OpenReadStream();
        var imagePath = await fileStorageService.UploadFileAsync(
            imageStream,
            uniqueFileName,
            "prescriptions",
            cancellationToken);

        // ── 3. Convert to Base64 ONLY for sending to Bedrock ──────────────
        // We still need the base64 string because the multimodal API needs it,
        // but we DO NOT save it in the database.
        using var memoryStream = new MemoryStream();
        imageStream.Position = 0; // Reset position to read again
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        // ── 4. Analyze with Bedrock ────────────────────────────────────────
        var extracted = await bedrockService.AnalyzeAsync<BedrockPrescriptionDto>(
            base64Image,
            PrescriptionPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No prescription data could be extracted from the uploaded image.");

        if (extracted.Medicines.Count == 0)
            throw new BadRequestException("No medicines could be extracted from the uploaded prescription image.");

        // ── 5. Parse the prescription date ────────────────────────────────
        var prescriptionDate = DateOnly.TryParseExact(
            extracted.PrescriptionDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        // ── 6. Build the domain entity (storing only relative path) ────────
        var prescription = new Prescription(
            userId: userId,
            doctorName: extracted.DoctorName ?? string.Empty,
            patientName: extracted.PatientName ?? string.Empty,
            prescriptionDate: prescriptionDate,
            imagePath: imagePath
        );

        // ── 7. Map every extracted medicine into PrescriptionMedicine entities ──
        foreach (var medicine in extracted.Medicines)
        {
            prescription.Medicines.Add(new PrescriptionMedicine
            {
                MedicineName = medicine.MedicineName ?? string.Empty,
                Dosage = medicine.Dosage ?? string.Empty,
                Frequency = medicine.Frequency ?? string.Empty,
                Duration = medicine.Duration ?? string.Empty,
                Notes = medicine.Notes
            });
        }

        // ── 8. Persist inside one transaction (EF tracks the whole graph) ──
        await prescriptionRepository.AddAsync(prescription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 9. Return the response ─────────────────────────────────────────
        return ApiResponse<PrescriptionResponseDto>.SuccessResponse(
            ToDto(prescription),
            "Prescription uploaded and analyzed successfully.");
    }

    // ── Mapping helper ─────────────────────────────────────────────────────
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
