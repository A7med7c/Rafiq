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
    IFileStorageService fileStorageService)
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

        using var imageStream = request.Image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockImagingReportDto>(
            base64Image,
            ImagingReportPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No imaging report data could be extracted from the uploaded image.");

        if (!extracted.IsValidDocument)
        {
            var detected = extracted.DetectedDocumentType;
            var detailMessage = string.IsNullOrWhiteSpace(detected) || detected == "Unknown"
                ? "The uploaded image could not be identified as a valid document."
                : $"Detected document type: {detected}.";

            throw new DocumentValidationException(
                "WRONG_DOCUMENT_TYPE_IMAGING_REPORT",
                $"The uploaded document is not an imaging report. {detailMessage} Please upload a valid imaging report image.");
        }

        if (extracted.IsUnreadable)
            throw new DocumentValidationException(
                "UNREADABLE_DOCUMENT_IMAGING_REPORT",
                "The imaging report image is unreadable. Please upload a clearer image or enter the information manually.");

        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var uploadStream = new MemoryStream(imageBytes);
        var imageUrl = await fileStorageService.UploadFileAsync(
            uploadStream,
            uniqueFileName,
            "imaging",
            cancellationToken);

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

        return ApiResponse<ImagingReportResponseDto>.SuccessResponse(
            preview,
            "Imaging report analyzed successfully. Review before saving.");
    }
}
