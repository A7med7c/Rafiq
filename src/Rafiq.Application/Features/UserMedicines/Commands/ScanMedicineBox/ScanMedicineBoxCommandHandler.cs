using MediatR;
using Rafiq.Application.AI.Prompts;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.UserMedicines.Commands.ScanMedicineBox;

public sealed class ScanMedicineBoxCommandHandler(
    ICurrentUserService currentUserService,
    IBedrockService bedrockService,
    IFileStorageService fileStorageService)
    : IRequestHandler<ScanMedicineBoxCommand, ApiResponse<ScanMedicineBoxResponseDto>>
{
    public async Task<ApiResponse<ScanMedicineBoxResponseDto>> Handle(
        ScanMedicineBoxCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        using var imageStream = request.Image.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var extracted = await bedrockService.AnalyzeAsync<BedrockMedicineBoxDto>(
            base64Image,
            MedicineBoxPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No medicine data could be extracted from the uploaded image.");

        if (!extracted.IsValidDocument)
        {
            var detected = extracted.DetectedDocumentType;
            var detailMessage = string.IsNullOrWhiteSpace(detected) || detected == "Unknown"
                ? "The uploaded image could not be identified as a valid document."
                : $"Detected document type: {detected}.";

            throw new DocumentValidationException(
                "WRONG_DOCUMENT_TYPE_MEDICINE_BOX",
                $"The uploaded image is not a medicine box or blister pack. {detailMessage} Please upload a valid medicine box image.");
        }

        if (extracted.IsUnreadable)
            throw new DocumentValidationException(
                "UNREADABLE_DOCUMENT_MEDICINE_BOX",
                "The medicine box image is unreadable. Please upload a clearer image or enter the information manually.");

        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var uploadStream = new MemoryStream(imageBytes);
        var imagePath = await fileStorageService.UploadFileAsync(
            uploadStream,
            uniqueFileName,
            "medicine-boxes",
            cancellationToken);

        var dto = new ScanMedicineBoxResponseDto
        {
            MedicineName = extracted.MedicineName,
            Strength = extracted.Strength,
            DosageForm = extracted.DosageForm,
            Manufacturer = extracted.Manufacturer,
            ImagePath = imagePath
        };

        return ApiResponse<ScanMedicineBoxResponseDto>.SuccessResponse(
            dto,
            "Medicine box scanned successfully. Please review and complete the missing information.");
    }
}
