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
    IAiTelemetryContext telemetryContext,
    IUsageIntelligenceService usageIntelligence,
    IDuplicateDocumentDetector duplicateDetector,
    IPrescriptionRepository prescriptionRepository)
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
        
        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        var uploadStream = new MemoryStream(imageBytes);
        var imagePath = await fileStorageService.UploadFileAsync(
            uploadStream,
            uniqueFileName,
            "prescriptions",
            cancellationToken);

        // ── Phase 1: Fast duplicate check ────────────────────────────
        var duplicateCheck = await duplicateDetector.ComputeHashAndCheckAsync(
            imageBytes,
            imagePath,
            profileId,
            currentUserId!.Value,
            cancellationToken);

        if (duplicateCheck.IsDuplicate)
        {
            if (duplicateCheck.IsSameProfile)
            {
                throw new DocumentValidationException("DUPLICATE_DOCUMENT", "This exact document has already been uploaded to this profile.");
            }
            
            if (!request.BypassFamilyDuplicateCheck)
            {
                return ApiResponse<PrescriptionResponseDto>.FailureResponse(
                    "This document already exists in another family member's profile.",
                    errorCode: "DuplicateInFamily",
                    errorData: new
                    {
                        existingProfileId = duplicateCheck.ExistingProfileId,
                        existingProfileName = duplicateCheck.ExistingProfileName
                    });
            }
        }
        // ─────────────────────────────────────────────────────────────

        var base64Image = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockPrescriptionDto>(
            base64Image,
            PrescriptionPrompt.Build(request.Language),
            LanguageSystemPrompt.Build(request.Language),
            cancellationToken)
            ?? throw new BadRequestException("No prescription data could be extracted from the uploaded image.");

        if (!extracted.IsValidDocument)
        {
            var detected = extracted.DetectedDocumentType ?? "Unknown";
            var detailMessage = string.IsNullOrWhiteSpace(detected) || detected == "Unknown"
                ? "The uploaded image could not be identified as a valid document."
                : $"Detected document type: {detected}.";

            var userId = currentUserService.UserId;
            if (userId.HasValue)
            {
                var isMedicalWrongType = detected.Contains("lab", StringComparison.OrdinalIgnoreCase)
                    || detected.Contains("imaging", StringComparison.OrdinalIgnoreCase)
                    || detected.Contains("report", StringComparison.OrdinalIgnoreCase);
                await usageIntelligence.SaveFlaggedRequestAsync(new(
                    UserId:         userId.Value,
                    RequestType:    "PrescriptionOcr",
                    UserRequest:    "User uploaded an image as a Prescription.",
                    AiResponse:     $"Rejected. {detailMessage}",
                    Classification: isMedicalWrongType ? "WrongDocumentType" : "NonMedicalUpload",
                    Reason:         $"Expected: Prescription. Detected: {detected}."),
                    cancellationToken);
            }

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

        var response = ApiResponse<PrescriptionResponseDto>.SuccessResponse(
            preview,
            "Prescription analyzed successfully. Review before saving.");

        // ── Phase 2: Smart similarity check ──────────────────────────
        var existsDuplicate = await prescriptionRepository.ExistsDuplicateAsync(
            profileId,
            prescriptionDate,
            extracted.DoctorName ?? string.Empty,
            extracted.PatientName ?? string.Empty,
            cancellationToken);

        if (existsDuplicate)
        {
            response = new ApiResponse<PrescriptionResponseDto>
            {
                Success = true,
                Message = response.Message,
                Data = response.Data,
                Warnings = new List<string> { "PossibleDuplicate" }
            };
        }
        // ─────────────────────────────────────────────────────────────

        return response;
    }
}
