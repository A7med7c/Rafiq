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
        // 1. Ensure user is authenticated
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        // 2. Save physical file via IFileStorageService to medicine-boxes directory
        var fileExtension = Path.GetExtension(request.Image.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        using var imageStream = request.Image.OpenReadStream();
        var imagePath = await fileStorageService.UploadFileAsync(
            imageStream,
            uniqueFileName,
            "medicine-boxes",
            cancellationToken);

        // 3. Convert to Base64 for sending to Bedrock
        using var memoryStream = new MemoryStream();
        imageStream.Position = 0;
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var base64Image = Convert.ToBase64String(memoryStream.ToArray());

        // 4. Analyze with Bedrock
        var extracted = await bedrockService.AnalyzeAsync<BedrockMedicineBoxDto>(
            base64Image,
            MedicineBoxPrompt.Build(),
            cancellationToken)
            ?? throw new BadRequestException("No medicine data could be extracted from the uploaded image.");

        // 5. Return extracted data to client for review (does not save to UserMedicine yet)
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
