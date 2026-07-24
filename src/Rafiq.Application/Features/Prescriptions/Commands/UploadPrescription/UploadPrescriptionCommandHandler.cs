using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;

public sealed class UploadPrescriptionCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService,
    IAiTelemetryContext telemetryContext)
    : IRequestHandler<UploadPrescriptionCommand, ApiResponse<PrescriptionResponseDto>>
{
    public async Task<ApiResponse<PrescriptionResponseDto>> Handle(
        UploadPrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var profileId = request.ProfileId;

        if (profileId == Guid.Empty)
        {
            var currentUserId = currentUserService.UserId
                ?? throw new UnauthorizedException("Authentication is required.");

            profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
                ?? throw new NotFoundException("PatientProfile", currentUserId);
        }

        await authorizationService.EnsureCanWriteAsync(profileId, cancellationToken);

        telemetryContext.Feature = Rafiq.Domain.Enums.AiFeature.PrescriptionOcr;
        telemetryContext.UserId  = currentUserService.UserId;

        using var imageStream = request.Image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockPrescriptionDto>(
            base64Image,
            PrescriptionPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No prescription data could be extracted from the uploaded image.");

        if (!extracted.IsValidDocument)
        {
            var detected = extracted.DetectedDocumentType;
            var detailMessage = string.IsNullOrWhiteSpace(detected) || detected == "Unknown"
                ? "The uploaded image could not be identified as a valid document."
                : $"Detected document type: {detected}.";

            throw new DocumentValidationException(
                "WRONG_DOCUMENT_TYPE_PRESCRIPTION",
                $"The uploaded document is not a prescription. {detailMessage} Please upload a valid prescription image.");
        }

        if (extracted.IsUnreadable)
            throw new DocumentValidationException(
                "UNREADABLE_DOCUMENT_PRESCRIPTION",
                "The prescription image is unreadable. Please upload a clearer image or enter the information manually.");

        if (extracted.Medicines.Count == 0)
            throw new BadRequestException("No medicines could be extracted from the uploaded prescription image.");

        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var uploadStream = new MemoryStream(imageBytes);
        var imagePath = await fileStorageService.UploadFileAsync(
            uploadStream,
            uniqueFileName,
            "prescriptions",
            cancellationToken);

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
