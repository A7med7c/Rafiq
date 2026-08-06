using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.ImagingReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.ImagingReports.Commands.UploadImagingReport;

public sealed class UploadImagingReportCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService,
    IAiTelemetryContext telemetryContext,
    IUsageIntelligenceService usageIntelligence,
    IDuplicateDocumentDetector duplicateDetector,
    IImagingReportRepository imagingReportRepository)
    : IRequestHandler<UploadImagingReportCommand, ApiResponse<ImagingReportResponseDto>>
{
    public async Task<ApiResponse<ImagingReportResponseDto>> Handle(
        UploadImagingReportCommand request,
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

        telemetryContext.Feature = Rafiq.Domain.Enums.AiFeature.ImagingOcr;
        telemetryContext.UserId  = currentUserService.UserId;

        using var imageStream = request.Image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        
        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        var uploadStream = new MemoryStream(imageBytes);
        var imageUrl = await fileStorageService.UploadFileAsync(
            uploadStream,
            uniqueFileName,
            "imaging",
            cancellationToken);

        // ── Phase 1: Fast duplicate check ────────────────────────────
        var duplicateCheck = await duplicateDetector.ComputeHashAndCheckAsync(
            imageBytes,
            imageUrl,
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
                return ApiResponse<ImagingReportResponseDto>.FailureResponse(
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

        var extracted = await bedrockService.AnalyzeAsync<BedrockImagingReportDto>(
            base64Image,
            ImagingReportPrompt.Build(request.Language),
            LanguageSystemPrompt.Build(request.Language),
            cancellationToken)
            ?? throw new BadRequestException("No imaging report data could be extracted from the uploaded image.");

        if (!extracted.IsValidDocument)
        {
            var detected = extracted.DetectedDocumentType ?? "Unknown";
            var detailMessage = string.IsNullOrWhiteSpace(detected) || detected == "Unknown"
                ? "The uploaded image could not be identified as a valid document."
                : $"Detected document type: {detected}.";

            var userId = currentUserService.UserId;
            if (userId.HasValue)
            {
                var isMedicalWrongType = detected.Contains("prescription", StringComparison.OrdinalIgnoreCase)
                    || detected.Contains("lab", StringComparison.OrdinalIgnoreCase)
                    || detected.Contains("report", StringComparison.OrdinalIgnoreCase);
                await usageIntelligence.SaveFlaggedRequestAsync(new(
                    UserId:         userId.Value,
                    RequestType:    "ImagingOcr",
                    UserRequest:    "User uploaded an image as an Imaging Report.",
                    AiResponse:     $"Rejected. {detailMessage}",
                    Classification: isMedicalWrongType ? "WrongDocumentType" : "NonMedicalUpload",
                    Reason:         $"Expected: Imaging Report. Detected: {detected}."),
                    cancellationToken);
            }

            throw new DocumentValidationException(
                "WRONG_DOCUMENT_TYPE_IMAGING_REPORT",
                $"The uploaded document is not an imaging report. {detailMessage} Please upload a valid imaging report image.");
        }

        if (extracted.IsUnreadable)
            throw new DocumentValidationException(
                "UNREADABLE_DOCUMENT_IMAGING_REPORT",
                "The imaging report image is unreadable. Please upload a clearer image or enter the information manually.");

        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var preview = new ImagingReportResponseDto
        {
            Id = Guid.Empty,
            ImagingType = extracted.ImagingType ?? "Unknown",
            BodyPart = extracted.BodyPart ?? "Unknown",
            Findings = extracted.Findings ?? "No findings reported.",
            Impression = extracted.Impression ?? "No impression reported.",
            DoctorName = extracted.DoctorName,
            ReportDate = reportDate.ToString("yyyy-MM-dd"),
            ImageUrl = imageUrl,
            OCRText = extracted.OcrText,
            Summary = extracted.AiSummary,
            CreatedAt = DateTime.UtcNow
        };

        var response = ApiResponse<ImagingReportResponseDto>.SuccessResponse(
            preview,
            "Imaging report analyzed successfully. Review before saving.");

        // ── Phase 2: Smart similarity check ──────────────────────────
        var existsDuplicate = await imagingReportRepository.ExistsDuplicateAsync(
            profileId,
            reportDate,
            extracted.ImagingType ?? string.Empty,
            extracted.BodyPart ?? string.Empty,
            cancellationToken);

        if (existsDuplicate)
        {
            response = new ApiResponse<ImagingReportResponseDto>
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
