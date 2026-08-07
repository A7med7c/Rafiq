using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.LabReports.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;
using System.Globalization;

namespace Rafiq.Application.Features.LabReports.Commands.UploadLabReport;

public sealed class UploadLabReportCommandHandler(
    ICurrentUserService currentUserService,
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService,
    IAiTelemetryContext telemetryContext,
    IUsageIntelligenceService usageIntelligence,
    IDuplicateDocumentDetector duplicateDetector,
    ILabReportRepository labReportRepository,
    IMedicalWarningCalculator warningCalculator)
    : IRequestHandler<UploadLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        UploadLabReportCommand request,
        CancellationToken cancellationToken)
    {
        var profileId = request.ProfileId;

        var currentUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        if (profileId == Guid.Empty)
        {
            profileId = (await patientProfileRepository.GetByUserIdAsync(currentUserId, cancellationToken))?.Id
                ?? throw new NotFoundException("PatientProfile", currentUserId);
        }

        await authorizationService.EnsureCanWriteAsync(profileId, cancellationToken);

        telemetryContext.Feature = Rafiq.Domain.Enums.AiFeature.LabOcr;
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
            "labs",
            cancellationToken);

        // ── Phase 1: Fast duplicate check ────────────────────────────
        var duplicateCheck = await duplicateDetector.ComputeHashAndCheckAsync(
            imageBytes,
            imageUrl,
            profileId,
            currentUserId, // Must exist due to EnsureCanWriteAsync check inside
            cancellationToken);

        if (duplicateCheck.IsDuplicate)
        {
            if (duplicateCheck.IsSameProfile)
            {
                throw new DocumentValidationException("DUPLICATE_DOCUMENT", "This exact document has already been uploaded to this profile.");
            }
            
            if (!request.BypassFamilyDuplicateCheck)
            {
                return ApiResponse<LabReportResponseDto>.FailureResponse(
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

        var extracted = await bedrockService.AnalyzeAsync<BedrockLabReportDto>(
            base64Image,
            LabReportPrompt.Build(request.Language),
            LanguageSystemPrompt.Build(request.Language),
            cancellationToken)
            ?? throw new BadRequestException("No lab report data could be extracted from the uploaded image.");

        if (!extracted.IsValidDocument)
        {
            var detected = extracted.DetectedDocumentType ?? "Unknown";
            var detailMessage = string.IsNullOrWhiteSpace(detected) || detected == "Unknown"
                ? "The uploaded image could not be identified as a valid document."
                : $"Detected document type: {detected}.";

            // Save a flag — the AI already determined this is the wrong document type.
            var userId = currentUserService.UserId;
            if (userId.HasValue)
            {
                var isMedicalWrongType = detected.Contains("prescription", StringComparison.OrdinalIgnoreCase)
                    || detected.Contains("imaging", StringComparison.OrdinalIgnoreCase)
                    || detected.Contains("report", StringComparison.OrdinalIgnoreCase);
                var classification = isMedicalWrongType ? "WrongDocumentType" : "NonMedicalUpload";
                await usageIntelligence.SaveFlaggedRequestAsync(new(
                    UserId:         userId.Value,
                    RequestType:    "LabOcr",
                    UserRequest:    "User uploaded an image as a Lab Report.",
                    AiResponse:     $"Rejected. {detailMessage}",
                    Classification: classification,
                    Reason:         $"Expected: Lab Report. Detected: {detected}."),
                    cancellationToken);
            }

            throw new DocumentValidationException(
                "WRONG_DOCUMENT_TYPE_LAB_REPORT",
                $"The uploaded document is not a lab report. {detailMessage} Please upload a valid laboratory report image.");
        }

        if (extracted.IsUnreadable)
            throw new DocumentValidationException(
                "UNREADABLE_DOCUMENT_LAB_REPORT",
                "The lab report image is unreadable. Please upload a clearer image or enter the information manually.");

        if (extracted.Tests.Count == 0)
            throw new BadRequestException("No laboratory tests could be extracted from the uploaded image.");

        var reportDate = DateOnly.TryParseExact(
            extracted.ReportDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var preview = new LabReportResponseDto
        {
            Id = Guid.Empty,
            LabName = extracted.LabName ?? string.Empty,
            DoctorName = extracted.DoctorName ?? string.Empty,
            ReportDate = reportDate.ToString("yyyy-MM-dd"),
            OCRText = extracted.OcrText,
            Summary = extracted.AiSummary,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            MedicalAttentionReason = extracted.MedicalAttentionReason,
            RecommendedSpecialty = extracted.RecommendedSpecialty,
            ConfidenceScore = extracted.ConfidenceScore,
            RequiresMedicalAttention = warningCalculator.RequiresMedicalAttention(extracted.ConfidenceScore),
            AttentionLevel = warningCalculator.ComputeAttentionLevel(extracted.ConfidenceScore).ToString(),
            Results = extracted.Tests.Select(test => new LabResultResponseDto
            {
                Id = Guid.NewGuid(),
                TestName = test.TestName ?? string.Empty,
                Value = test.Value ?? string.Empty,
                Unit = test.Unit ?? string.Empty,
                NormalRange = test.NormalRange ?? string.Empty,
                Status = test.Status
            }).ToList()
        };

        var response = ApiResponse<LabReportResponseDto>.SuccessResponse(
            preview,
            "Lab report analyzed successfully. Review before saving.");

        // ── Phase 2: Smart similarity check ──────────────────────────
        var existsDuplicate = await labReportRepository.ExistsDuplicateAsync(
            profileId,
            reportDate,
            extracted.LabName ?? string.Empty,
            extracted.DoctorName ?? string.Empty,
            cancellationToken);

        if (existsDuplicate)
        {
            response = new ApiResponse<LabReportResponseDto>
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
