using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.GeneralDocuments.Commands.UploadGeneralDocument;
using Rafiq.Domain.Exceptions;

public sealed class UploadGeneralDocumentCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService)
    : IRequestHandler<
        UploadGeneralDocumentCommand,
        ApiResponse<GeneralDocumentPreviewDto>>
{
    public async Task<ApiResponse<GeneralDocumentPreviewDto>> Handle(
        UploadGeneralDocumentCommand request,
        CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
    ?? throw new UnauthorizedException(
        "Authentication is required.");

        var extension = Path.GetExtension(request.Image.FileName);

        var fileName = $"{Guid.NewGuid()}{extension}";

        using var imageStream = request.Image.OpenReadStream();

        var imagePath =
            await fileStorageService.UploadFileAsync(
                imageStream,
                fileName,
                "general-documents",
                cancellationToken);

        using var memory = new MemoryStream();

        imageStream.Position = 0;

        await imageStream.CopyToAsync(
            memory,
            cancellationToken);

        var base64 =
            Convert.ToBase64String(
                memory.ToArray());

        var extracted = await bedrockService.AnalyzeAsync<BedrockGeneralDocumentDto>(
            base64,
            GeneralDocumentPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("Unable to analyze document.");

        return ApiResponse<GeneralDocumentPreviewDto>.SuccessResponse(
            new()
            {
                DocumentTitle = extracted.DocumentTitle ?? "Medical Document",

                DocumentType = extracted.DocumentType,

                DoctorName = extracted.DoctorName,

                HospitalOrClinic = extracted.HospitalOrClinic,

                DocumentDate = extracted.DocumentDate,

                AiSummary = extracted.AiSummary,

                ImagePath = imagePath
            },

    "Document analyzed successfully.");

    }
}