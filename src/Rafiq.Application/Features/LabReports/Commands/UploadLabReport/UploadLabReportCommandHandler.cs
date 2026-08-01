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
    IUsageIntelligenceService usageIntelligence)
    : IRequestHandler<UploadLabReportCommand, ApiResponse<LabReportResponseDto>>
{
    public async Task<ApiResponse<LabReportResponseDto>> Handle(
        UploadLabReportCommand request,
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

        telemetryContext.Feature = Rafiq.Domain.Enums.AiFeature.LabOcr;
        telemetryContext.UserId  = currentUserService.UserId;

        using var imageStream = request.Image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockLabReportDto>(
            base64Image,
            LabReportPrompt.Build(),
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

        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var uploadStream = new MemoryStream(imageBytes);
        var imageUrl = await fileStorageService.UploadFileAsync(
            uploadStream,
            uniqueFileName,
            "labs",
            cancellationToken);

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
            Summary = extracted.Summary,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
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

        return ApiResponse<LabReportResponseDto>.SuccessResponse(
            preview,
            "Lab report analyzed successfully. Review before saving.");
    }
}
