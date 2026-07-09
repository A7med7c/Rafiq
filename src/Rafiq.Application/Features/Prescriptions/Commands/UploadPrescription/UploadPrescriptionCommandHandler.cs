using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Exceptions;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;

public sealed class UploadPrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadPrescriptionCommand, ApiResponse<PrescriptionResponseDto>>
{
    public async Task<ApiResponse<PrescriptionResponseDto>> Handle(
        UploadPrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var imageStream = request.Image.OpenReadStream();
        var imagePath = await fileStorageService.UploadFileAsync(
            imageStream,
            uniqueFileName,
            "prescriptions",
            cancellationToken);

        using var memoryStream = new MemoryStream();
        imageStream.Position = 0;
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        var extracted = await bedrockService.AnalyzeAsync<BedrockPrescriptionDto>(
            base64Image,
            PrescriptionPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No prescription data could be extracted from the uploaded image.");

        if (extracted.Medicines.Count == 0)
            throw new BadRequestException("No medicines could be extracted from the uploaded prescription image.");

        var prescriptionDate = DateOnly.TryParseExact(
            extracted.PrescriptionDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var preview = new PrescriptionResponseDto
        {
            Id = Guid.Empty,
            DoctorName = extracted.DoctorName ?? string.Empty,
            PatientName = extracted.PatientName ?? string.Empty,
            PrescriptionDate = prescriptionDate.ToString("yyyy-MM-dd"),
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow,
            Medicines = extracted.Medicines.Select(medicine => new PrescriptionMedicineResponseDto
            {
                Id = Guid.NewGuid(),
                MedicineName = medicine.MedicineName ?? string.Empty,
                Dosage = medicine.Dosage ?? string.Empty,
                Frequency = medicine.Frequency ?? string.Empty,
                Duration = medicine.Duration ?? string.Empty,
                Notes = medicine.Notes
            }).ToList()
        };

        return ApiResponse<PrescriptionResponseDto>.SuccessResponse(
            preview,
            "Prescription analyzed successfully. Review before saving.");
    }
}
