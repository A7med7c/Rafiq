using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocument;
using Rafiq.Domain.Exceptions;

public sealed class UploadGeneralDocumentCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService,
    IAiTelemetryContext telemetryContext)
    : IRequestHandler<
        UploadGeneralDocumentCommand,
        ApiResponse<GeneralDocumentPreviewDto>>
{
    public async Task<ApiResponse<GeneralDocumentPreviewDto>> Handle(
        UploadGeneralDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        telemetryContext.Feature = Rafiq.Domain.Enums.AiFeature.GeneralDocOcr;
        telemetryContext.UserId  = userId;

        using var memory = new MemoryStream();
        using (var sourceStream = request.Image.OpenReadStream())
            await sourceStream.CopyToAsync(memory, cancellationToken);
        var imageBytes = memory.ToArray();
        var base64 = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockGeneralDocumentDto>(
            base64,
            GeneralDocumentPrompt.Build(),
            LanguageSystemPrompt.Build("en"),
            cancellationToken)
            ?? throw new BadRequestException("Unable to analyze document.");

        var category = (extracted.DocumentCategory ?? string.Empty).Trim().ToLowerInvariant();

        if (category == "lab")
            throw new DocumentValidationException(
                "SHOULD_BE_LAB_REPORT",
                "This looks like a lab report. Please upload it in the Lab Analysis section.");

        if (category == "imaging")
            throw new DocumentValidationException(
                "SHOULD_BE_IMAGING_REPORT",
                "This looks like a radiology/imaging report. Please upload it in the X-Ray & Imaging section.");

        if (category == "prescription")
            throw new DocumentValidationException(
                "SHOULD_BE_PRESCRIPTION",
                "This looks like a prescription. Please upload it in the Prescriptions section.");

        if (category == "medicine")
            throw new DocumentValidationException(
                "SHOULD_BE_MEDICINE_BOX",
                "This looks like a medicine box. Please scan it using the Medicine Box section.");

        if (category == "not_medical")
            throw new DocumentValidationException(
                "NOT_MEDICAL_DOCUMENT",
                "The uploaded image does not appear to be a medical document.");

        var extension = Path.GetExtension(request.Image.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var imagePath = await fileStorageService.UploadFileAsync(
            new MemoryStream(imageBytes),
            fileName,
            "general-documents",
            cancellationToken);

        return ApiResponse<GeneralDocumentPreviewDto>.SuccessResponse(
            new()
            {
                DocumentTitle = extracted.DocumentTitle ?? "Medical Document",
                DocumentType = extracted.DocumentType,
                DoctorName = extracted.DoctorName,
                HospitalOrClinic = extracted.HospitalOrClinic,
                DocumentDate = extracted.DocumentDate,
                AiSummary = extracted.AiSummary,
                OcrText = extracted.OcrText,
                ImagePath = imagePath
            },
            "Document analyzed successfully.");
    }
}